using UnityEngine;
using UnityEngine.UIElements;

namespace MoriMonchiSimulator
{
    public static class MonchiPortraitUI
    {
        private static readonly Color FallbackEmpty = new Color(0.24f, 0.24f, 0.28f);

        public static void Apply(VisualElement element, CreatureDNA dna)
        {
            if (element == null) return;

            var tex = MonchiPortraitService.Instance != null && dna != null
                ? MonchiPortraitService.Instance.GetPortrait(dna)
                : null;

            if (tex != null)
            {
                element.style.backgroundImage = new StyleBackground(tex);
                element.style.backgroundColor = Color.clear;
                element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            }
            else
            {
                element.style.backgroundImage = StyleKeyword.Null;
                element.style.backgroundColor = dna != null ? dna.BaseColor : FallbackEmpty;
            }
        }

        public static void ApplyHeadshot(VisualElement element, CreatureDNA dna)
        {
            if (element == null) return;

            var tex = MonchiPortraitService.Instance != null && dna != null
                ? MonchiPortraitService.Instance.GetHeadshot(dna)
                : null;

            if (tex != null)
            {
                element.style.backgroundImage = new StyleBackground(tex);
                element.style.backgroundColor = Color.clear;
                element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            }
            else
            {
                element.style.backgroundImage = StyleKeyword.Null;
                element.style.backgroundColor = dna != null ? dna.BaseColor : FallbackEmpty;
            }
        }

        public static void ApplyLive(VisualElement element, CreatureDNA dna)
        {
            if (element == null) return;
            var live = MonchiLivePortrait.Instance;
            if (live != null && dna != null && live.Begin(element, dna)) return;
            Apply(element, dna);
        }

        public static void Apply(UnityEngine.UI.Image image, CreatureDNA dna)
        {
            if (image == null) return;

            var sprite = MonchiPortraitService.Instance != null && dna != null
                ? MonchiPortraitService.Instance.GetPortraitSprite(dna)
                : null;

            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
                image.preserveAspect = true;
            }
            else
            {
                image.sprite = null;
                image.color = dna != null ? dna.BaseColor : FallbackEmpty;
            }
        }
    }
}
