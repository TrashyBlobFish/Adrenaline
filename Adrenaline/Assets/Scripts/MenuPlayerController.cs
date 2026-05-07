using UnityEngine;

public class MenuPlayerController : MonoBehaviour
{
    public Animator menuAnimator;
    public Animator playerAnimator;
    public ParticleSystem particleSystem;
    public float slowSpeed = 3f;
    private float currentMenuAnimatorSpeed = 1f;
    private float currentPlayerAnimatorSpeed = 1f;
    private float currentParticleSpeed = 1f;
    private float targetSlowmoSpeed = 1f;
    private bool isSlowing = false;

    void Update()
    {
        if (isSlowing)
        {
            ApplySlowmoSpeed();
        }
    }

    private void ApplySlowmoSpeed()
    {
        // Gradually adjust menu animator speed from current to target
        if (menuAnimator != null)
        {
            currentMenuAnimatorSpeed = Mathf.Lerp(currentMenuAnimatorSpeed, targetSlowmoSpeed, slowSpeed * Time.deltaTime);
            if (Mathf.Abs(currentMenuAnimatorSpeed - targetSlowmoSpeed) < 0.001f)
                currentMenuAnimatorSpeed = targetSlowmoSpeed;
            menuAnimator.speed = currentMenuAnimatorSpeed;
        }

        // Gradually adjust player animator speed from current to target
        if (playerAnimator != null)
        {
            currentPlayerAnimatorSpeed = Mathf.Lerp(currentPlayerAnimatorSpeed, targetSlowmoSpeed, slowSpeed * Time.deltaTime);
            if (Mathf.Abs(currentPlayerAnimatorSpeed - targetSlowmoSpeed) < 0.001f)
                currentPlayerAnimatorSpeed = targetSlowmoSpeed;
            playerAnimator.speed = currentPlayerAnimatorSpeed;
        }

        // Gradually adjust particle system speed
        if (particleSystem != null)
        {
            currentParticleSpeed = Mathf.Lerp(currentParticleSpeed, targetSlowmoSpeed, slowSpeed * Time.deltaTime);
            if (Mathf.Abs(currentParticleSpeed - targetSlowmoSpeed) < 0.001f)
                currentParticleSpeed = targetSlowmoSpeed;

            if (targetSlowmoSpeed <= 0f && currentParticleSpeed <= 0.001f)
            {
                if (particleSystem.isPlaying)
                    particleSystem.Pause();
            }
            else if (!particleSystem.isPlaying && targetSlowmoSpeed > 0f)
            {
                particleSystem.Play();
            }

            // Adjust playback speed smoothly
            var mainModule = particleSystem.main;
            mainModule.simulationSpeed = currentParticleSpeed;
        }

        // Stop slowing when target is reached
        if (Mathf.Abs(currentMenuAnimatorSpeed - targetSlowmoSpeed) < 0.001f &&
            Mathf.Abs(currentPlayerAnimatorSpeed - targetSlowmoSpeed) < 0.001f &&
            Mathf.Abs(currentParticleSpeed - targetSlowmoSpeed) < 0.001f)
        {
            isSlowing = false;
        }
    }

    public void InitializeSlowmo(Animator menuAnim, Animator playerAnim, ParticleSystem particles)
    {
        menuAnimator = menuAnim;
        playerAnimator = playerAnim;
        particleSystem = particles;
    }

    public void SlowToHalt()
    {
        targetSlowmoSpeed = 0f;
        isSlowing = true;
    }

    public void ResumeAll()
    {
        targetSlowmoSpeed = 1f;
        isSlowing = true;
    }

    public void ClearParticles()
    {
        if (particleSystem != null)
        {
            particleSystem.Clear();
            particleSystem.Play();
            currentParticleSpeed = 1f;
            var mainModule = particleSystem.main;
            mainModule.simulationSpeed = 1f;
        }
    }

    public void PlayAnimation(string triggerName)
    {
        if (menuAnimator != null)
            menuAnimator.SetTrigger(triggerName);

        if (playerAnimator != null)
            playerAnimator.SetTrigger(triggerName);
    }

    public void PlayAnimationOnMenu(string triggerName)
    {
        if (menuAnimator != null)
        {
            menuAnimator.speed = 1f;
            menuAnimator.SetTrigger(triggerName);
        }
        
        // Resume everything to normal speed
        ResumeAll();
    }

    public void PlayAnimationOnPlayer(string triggerName)
    {
        if (playerAnimator != null)
        {
            playerAnimator.speed = 1f;
            playerAnimator.SetTrigger(triggerName);
        }
        
        // Resume everything to normal speed
        ResumeAll();
    }

    public float GetCurrentSlowmoSpeed()
    {
        return currentMenuAnimatorSpeed;
    }
}
