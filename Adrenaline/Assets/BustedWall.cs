using UnityEngine;

public class BustedWall : MonoBehaviour
{
    [SerializeField] private GameObject bustedWallPrefab;
    [SerializeField] private GameObject FullWallPrefab;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject.CompareTag("Shield"))
        {
            if (!bustedWallPrefab.activeSelf)
            {
                gameObject.GetComponent<BoxCollider>().enabled = false;
                bustedWallPrefab.SetActive(true);
                FullWallPrefab.SetActive(false);
                Destroy(bustedWallPrefab, 3f);
            }
            
        }
    }
}
