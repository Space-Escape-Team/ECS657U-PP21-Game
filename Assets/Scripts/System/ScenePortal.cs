using UnityEngine;

public class ScenePortal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GetComponent<SceneLoader>().LoadScene();
        }
    }
}
