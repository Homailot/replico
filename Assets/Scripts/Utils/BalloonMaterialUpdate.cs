using UnityEngine;

namespace Utils
{
    public class BalloonMaterialUpdate : MonoBehaviour
    {
        [SerializeField] private Mesh balloon1Mesh;
        [SerializeField] private Mesh balloon2Mesh;
        
        [SerializeField] private Material balloon1Material;
        [SerializeField] private Material balloon2Material;
        [SerializeField] private Material balloonNoneMaterial;
        
        [SerializeField] private Material balloon1MaterialBehind;
        [SerializeField] private Material balloon2MaterialBehind;
        [SerializeField] private Material balloonNoneMaterialBehind;
        
        private static readonly int Player1 = Shader.PropertyToID("_Player");
        private static readonly int Color1 = Shader.PropertyToID("_Color");
        private static readonly int Color2 = Shader.PropertyToID("_Color2");

        public void UpdateBalloonNone(GameObject balloon, ulong playerId)
        {
            balloonNoneMaterial.SetInt(Player1, (int) playerId);
            balloonNoneMaterial.SetColor(Color1, playerId == 0 ? balloon1Material.GetColor(Color1) : balloon2Material.GetColor(Color1));
            balloonNoneMaterial.SetColor(Color2,
                playerId == 0 ? balloon1Material.GetColor(Color2) : balloon2Material.GetColor(Color2));
            
            balloonNoneMaterialBehind.SetInt(Player1, (int) playerId);
            balloonNoneMaterialBehind.SetColor(Color1, playerId == 0 ? balloon1MaterialBehind.GetColor(Color1) : balloon2MaterialBehind.GetColor(Color1));
            balloonNoneMaterialBehind.SetColor(Color2,
                playerId == 0 ? balloon1MaterialBehind.GetColor(Color2) : balloon2MaterialBehind.GetColor(Color2));
            
            var meshFilter = balloon.GetComponent<MeshFilter>();
            if (meshFilter == null) return;
            
            meshFilter.mesh = playerId switch
            {
                0 => balloon1Mesh,
                1 => balloon2Mesh,
                _ => meshFilter.mesh
            };
        }
        
        public void UpdateBalloonLayer(GameObject balloonGameObject, ulong playerId)
        {
            balloonGameObject.layer = playerId switch
            {
                0 => LayerMask.NameToLayer("Balloon"),
                1 => LayerMask.NameToLayer("Balloon2"),
                _ => balloonGameObject.layer
            }; 
            
            var meshFilter = balloonGameObject.GetComponent<MeshFilter>();
            
            if (meshFilter == null) return;
            
            meshFilter.mesh = playerId switch
            {
                0 => balloon1Mesh,
                1 => balloon2Mesh,
                _ => meshFilter.mesh
            };
        }
    }
}