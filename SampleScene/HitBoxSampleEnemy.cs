using UnityEngine;

public class HitBoxSampleEnemy : MonoBehaviour
{
    private void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.tag == "atk")
        {
            Debug.Log("Enemy is attacked");
        }
    }
}
