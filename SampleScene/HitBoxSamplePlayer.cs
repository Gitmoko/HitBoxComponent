using UnityEngine;

public class HitBoxSamplePlayer : MonoBehaviour
{
    private SimpleAnimation anim;

    // Start is called before the first frame update
    private void Start()
    {
        anim = GetComponent<SimpleAnimation>();
    }

    // Update is called once per frame
    private void Update()
    {

        if (((int)Time.time) % 3 == 0)
        {
            anim.Stop();
            anim.Play("atk");
        }

    }
}
