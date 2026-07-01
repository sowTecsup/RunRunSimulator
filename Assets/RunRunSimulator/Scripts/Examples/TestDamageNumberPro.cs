using DamageNumbersPro;
using Sirenix.OdinInspector;
using UnityEngine;

public class TestDamageNumberPro : MonoBehaviour
{
    public DamageNumber numberPrefab;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    [Button]
    public void SpawnDamageNumber(float damage)
    {
        DamageNumber damageNumber = numberPrefab.Spawn(transform.position, Random.Range(1, 100));
    }
    /* Spawn Functions Mesh
    //Basic:
DamageNumber damageNumber = numberPrefab.Spawn();
DamageNumber damageNumber = numberPrefab.Spawn(Vector3 position);
DamageNumber damageNumber = numberPrefab.Spawn(Vector3 position, Transform followedTransform);

//Spawn and Set Number:
DamageNumber damageNumber = numberPrefab.Spawn(Vector3 position, float number);
DamageNumber damageNumber = numberPrefab.Spawn(Vector3 position, float number, Transform followedTransform);

//Spawn and Set Text:
DamageNumber damageNumber = numberPrefab.Spawn(Vector3 position, string leftText);
DamageNumber damageNumber = numberPrefab.Spawn(Vector3 position, string leftText, Transform followedTransform);
     
     */
    /* Utiliy functions 
   DamageNumber damageNumber = numberPrefab.Spawn(Vector3 position);

//Set Followed Target:
damageNumber.SetFollowedTarget(Transform followedTransform);

//Set Color:
damageNumber.SetColor(Color color);

//Set Gradient Color:
damageNumber.SetGradientColor(VertexGradient vertexGradient);
damageNumber.SetGradientColor(Color topLeft, Color topRight, Color bottomLeft, Color bottomRight);

//Set Random Color:
damageNumber.SetRandomColor(Color from, Color to);
damageNumber.SetRandomColor(Gradient gradient);

//Font Material:
damageNumber.SetFontMaterial(TMP_FontAsset font);
TMP_FontAsset font = damageNumber.GetFontMaterial();

//Set Scale:
damageNumber.SetScale(float scale);

//Prewarm Pooling (call this once):
damageNumber.PrewarmPool();

    */
}
