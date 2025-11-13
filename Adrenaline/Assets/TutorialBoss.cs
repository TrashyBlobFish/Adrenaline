using UnityEngine;

public class TutorialBoss : MonoBehaviour
{
    public Transform player;
    public Material phase1Material;
    public Material phase2Material;
    public Material phase3Material;
    public ParticleSystem phase2Particles;
    public GameObject bulletPrefab;
    public GameObject PhaseTwoMap;
    public float bulletSpeed = 20f;
    public float spinSpeed = 180f;
    public float shootInterval = 2f;
    public float phaseDuration = 10f; // seconds per phase
    public float jumpForce = 15f;
    private float jumpTimer = 0f;
    public float jumpInterval = 3f;

    private int phase = 1;
    private float phaseTimer = 0f;
    private float shootTimer = 0f;
    private Renderer rend;
    private Rigidbody rb;
    private bool hasJumped = false;

    void Start()
    {
        rend = GetComponent<Renderer>();
        rb = GetComponent<Rigidbody>();
        SetPhase(1);
        player = GameObject.Find("Player").transform;

    }

    void Update()
    {
        if (player == null) return;

        phaseTimer += Time.deltaTime;

        // Phase behaviors
        if (phase == 1)
        {
            PhaseTwoMap.SetActive(false);
            LookAtPlayer();
            shootTimer += Time.deltaTime;
            if (shootTimer >= shootInterval)
            {
                ShootAtPlayer();
                shootTimer = 0f;
            }
        }
        else if (phase == 2)
        {
            PhaseTwoMap.SetActive(true);

            // Spin in circles instead of looking at the player
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);

            if (!phase2Particles.isPlaying)
            {
                phase2Particles.Play();
            }
            var shape = phase2Particles.shape;
            shape.rotation = new Vector3(0, transform.eulerAngles.y, 0);
        }
        else if (phase == 3)
        {
            jumpTimer += Time.deltaTime;
            PhaseTwoMap.SetActive(false);
            if (jumpTimer >= jumpInterval)
            {
                JumpAtPlayer();
                jumpTimer = 0f;
            }
        }

        if (phase == 3)
        {
            if (transform.position.y <= 150f)
            {
                Destroy(gameObject);
            }
        }
        if (player.transform.position.y <= 150)
        {
            player.transform.position = GameObject.Find("Boss Respawn Point").transform.position;
            SetPhase(1);
        }
    }

    public void SetPhase(int newPhase)
    {
        phase = newPhase;
        phaseTimer = 0f;
        shootTimer = 0f;
        hasJumped = false;

        if (phase == 1)
        {
            rend.material = phase1Material;
            if (phase2Particles.isPlaying) phase2Particles.Stop();
        }
        else if (phase == 2)
        {
            rend.material = phase2Material;
        }
        else if (phase == 3)
        {
            rend.material = phase3Material;
            jumpTimer = 0f;
            if (phase2Particles.isPlaying) phase2Particles.Stop();
        }
    }

    void LookAtPlayer()
    {
        
        if (player == null) return;
        Debug.Log("Looking at player");
        Vector3 targetPosition = player.position;
        targetPosition.y = transform.position.y; // Keep boss upright
        transform.LookAt(targetPosition);
    }

    void ShootAtPlayer()
    {
        if (bulletPrefab == null) return;
        GameObject bullet = Instantiate(bulletPrefab, transform.position + transform.forward * 4f, Quaternion.identity);
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            bulletRb.linearVelocity = dir * bulletSpeed;
        }
        // Optional: Ignore collision with self
        Collider bossCollider = GetComponent<Collider>();
        Collider bulletCollider = bullet.GetComponent<Collider>();
        if (bossCollider != null && bulletCollider != null)
        {
            Physics.IgnoreCollision(bulletCollider, bossCollider);
            StartCoroutine(ReenableCollisionAfterDelay(bulletCollider, bossCollider, 0.75f));
        }
    }

    private System.Collections.IEnumerator ReenableCollisionAfterDelay(Collider bulletCollider, Collider bossCollider, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bulletCollider != null && bossCollider != null)
        {
            Physics.IgnoreCollision(bulletCollider, bossCollider, false);
        }
    }

    void JumpAtPlayer()
    {
        if (rb == null) return;
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0.5f; // Add upward force
        rb.AddForce(dir.normalized * jumpForce, ForceMode.Impulse);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            player.transform.position = GameObject.Find("Boss Respawn Point").transform.position;
            SetPhase(1);
        }
    }
}
