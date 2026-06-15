using UnityEngine;

public class Explosion : MonoBehaviour
{
    public GameObject explosionEffect;
    public void Explode()
    {
        print("Boom");
        GameObject explosionInstance = Instantiate(explosionEffect);
        explosionInstance.transform.SetParent(transform, false);
        explosionInstance.GetComponent<ParticleSystem>().Play();
    }
}