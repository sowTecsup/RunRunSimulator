using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public partial class MorimonchiDetailInfoUITK
{
    // ── Lineage tab (ancestor tree: self + parents + grandparents) ──

    private void BuildLineage(CreatureDNA dna)
    {
        if (lineageTree == null) return;
        lineageTree.Clear();

        bool hasAncestry = !string.IsNullOrEmpty(dna.MotherID) || !string.IsNullOrEmpty(dna.FatherID);
        if (lineageEmpty != null) lineageEmpty.style.display = hasAncestry ? DisplayStyle.None : DisplayStyle.Flex;

        // depth 2 → self (chip) + parents + grandparents.
        lineageTree.Add(BuildBlock(dna, "Tú", depth: 2, isSelf: true));
    }

    // A generation block stacks vertically: [parents row] → [vertical connector] →
    // [this creature's chip]. Recurses upward until depth runs out or ancestry ends.
    private VisualElement BuildBlock(CreatureDNA dna, string role, int depth, bool isSelf)
    {
        var block = new VisualElement();
        block.AddToClassList("tree-block");

        bool hasParents = dna != null &&
            (!string.IsNullOrEmpty(dna.MotherID) || !string.IsNullOrEmpty(dna.FatherID));

        if (depth > 0 && hasParents)
        {
            var parents = new VisualElement();
            parents.AddToClassList("tree-parents");
            parents.Add(WrapBranch(BuildAncestor(dna.MotherID, "Madre", depth - 1)));
            parents.Add(WrapBranch(BuildAncestor(dna.FatherID, "Padre", depth - 1)));
            block.Add(parents);

            var conn = new VisualElement();
            conn.AddToClassList("tree-connector-v");
            block.Add(conn);
        }

        block.Add(MakeChip(dna, role, isSelf, dead: false));
        return block;
    }

    private static VisualElement WrapBranch(VisualElement child)
    {
        var b = new VisualElement();
        b.AddToClassList("tree-branch");
        b.Add(child);
        return b;
    }

    // Resolves an ancestor by ID. Known (in registry) → full recursion upward.
    // Unknown (dead/removed) → genetics parsed from the ID, no further ancestry.
    private VisualElement BuildAncestor(string id, string role, int depth)
    {
        if (string.IsNullOrEmpty(id))
            return BuildBlock(null, role, depth, isSelf: false);

        if (registry != null && registry.TryGet(id, out var known))
            return BuildBlock(known, role, depth, isSelf: false);

        var block = new VisualElement();
        block.AddToClassList("tree-block");
        block.Add(MakeChip(ParseGenetics(id), role, isSelf: false, dead: true));
        return block;
    }

    private VisualElement MakeChip(CreatureDNA dna, string role, bool isSelf, bool dead)
    {
        var chip = new VisualElement();
        chip.AddToClassList("tree-chip");
        if (isSelf)     chip.AddToClassList("tree-chip--self");
        if (dna == null) chip.AddToClassList("tree-chip--unknown");
        if (dead || (dna != null && dna.IsDead)) chip.AddToClassList("tree-dead");

        var sw = new VisualElement();
        sw.AddToClassList("tree-swatch");
        sw.style.backgroundColor = dna != null ? dna.BaseColor : new Color(0.2f, 0.2f, 0.25f);
        chip.Add(sw);

        var name = new Label(ChipName(dna));
        name.AddToClassList("tree-name");
        chip.Add(name);

        var r = new Label(role);
        r.AddToClassList("tree-role");
        chip.Add(r);
        return chip;
    }

    private string ChipName(CreatureDNA dna)
    {
        if (dna == null) return "¿?";
        if (!string.IsNullOrEmpty(dna.CustomName)) return dna.CustomName;
        return database != null ? dna.GetDisplayName(database) : dna.ToStringID();
    }

    // A UniqueID is "<genetic_string>-<timestamp>"; strip the timestamp and parse
    // the genetics so a missing ancestor still shows its color and parts.
    private static CreatureDNA ParseGenetics(string uniqueId)
    {
        int li = uniqueId.LastIndexOf('-');
        return CreatureDNA.FromID(li > 0 ? uniqueId.Substring(0, li) : uniqueId);
    }

    // ── Breed tab (descendants: partners + their children) ─────────

    // Children are found by scanning the registry for anyone whose Mother/Father is
    // this creature (robust whether or not ChildrenIDs is maintained), then grouped
    // by the OTHER parent (the partner). Tree grows downward: self → partners → kids.
    private void BuildBreed(CreatureDNA dna)
    {
        if (breedTree == null) return;
        breedTree.Clear();

        string selfId = dna.UniqueID;
        var byPartner = new Dictionary<string, List<CreatureDNA>>();
        var order     = new List<string>();   // preserves discovery order

        if (registry != null && !string.IsNullOrEmpty(selfId))
            foreach (var c in registry.GetAll().Values)
            {
                bool isMom = c.MotherID == selfId;
                bool isDad = c.FatherID == selfId;
                if (!isMom && !isDad) continue;

                string partnerId = (isMom ? c.FatherID : c.MotherID) ?? "";
                if (!byPartner.TryGetValue(partnerId, out var list))
                {
                    list = new List<CreatureDNA>();
                    byPartner[partnerId] = list;
                    order.Add(partnerId);
                }
                list.Add(c);
            }

        bool any = order.Count > 0;
        if (breedEmpty != null) breedEmpty.style.display = any ? DisplayStyle.None : DisplayStyle.Flex;
        if (!any) return;

        var root = new VisualElement();
        root.AddToClassList("tree-block");
        root.Add(MakeChip(dna, "Tú", isSelf: true, dead: false));

        var conn = new VisualElement();
        conn.AddToClassList("tree-connector-v");
        root.Add(conn);

        var partnersRow = new VisualElement();
        partnersRow.AddToClassList("tree-children");
        foreach (var pid in order)
            partnersRow.Add(BuildPartnerBranch(pid, byPartner[pid]));
        root.Add(partnersRow);

        breedTree.Add(root);
    }

    private VisualElement BuildPartnerBranch(string partnerId, List<CreatureDNA> kids)
    {
        var col = new VisualElement();
        col.AddToClassList("tree-partner");

        CreatureDNA pdna = null;
        bool        dead = false;
        if (!string.IsNullOrEmpty(partnerId))
        {
            if (registry != null && registry.TryGet(partnerId, out var known)) pdna = known;
            else { pdna = ParseGenetics(partnerId); dead = true; }
        }

        var pchip = MakeChip(pdna, "Pareja", isSelf: false, dead: dead);
        pchip.AddToClassList("tree-chip--partner");
        col.Add(pchip);

        var conn = new VisualElement();
        conn.AddToClassList("tree-connector-v");
        col.Add(conn);

        var kidsRow = new VisualElement();
        kidsRow.AddToClassList("tree-children");
        foreach (var kid in kids)
        {
            var branch = new VisualElement();
            branch.AddToClassList("tree-branch");
            branch.Add(MakeChip(kid, "Cría", isSelf: false, dead: false));
            kidsRow.Add(branch);
        }
        col.Add(kidsRow);
        return col;
    }
}
