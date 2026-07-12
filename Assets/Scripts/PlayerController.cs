using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;

    Rigidbody rb;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        float hValue = Input.GetAxis("Horizontal");
        float vValue = Input.GetAxis ("Vertical");

        //Debug.DrawRay(transform.position, transform.forward * 2f, Color.blue);

        Vector3 movement = new Vector3(hValue * speed, rb.linearVelocity.y, vValue * speed);
        rb.linearVelocity = movement;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
        }
}

    private void OnDrawGizmosSelected()
    {
        Debug.DrawRay(transform.position, transform.forward * 2f, Color.blue);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Player collided with enemy");
            //Handle enemy collisions
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Win"))
        {
            SceneManager.LoadScene("GameEnd");
            //Handle player pickups.
            //Destroy(other.gameObject);
        }
    }
}
