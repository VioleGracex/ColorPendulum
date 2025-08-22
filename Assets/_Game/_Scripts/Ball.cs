using UnityEngine;
using DG.Tweening;

public class Ball : MonoBehaviour
{
    public BallColor color;
    public SpriteRenderer spriteRenderer;
    // public Sprite[] colorSprites; // No longer needed
    public ParticleSystem clearEffect;

    // True if this ball has been successfully stacked in a tube
    public bool placedInTube = false;

    private Rigidbody2D rb;
    private bool isFalling = false;

    public void SetColor(BallColor c)
    {
        color = c;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color.ToColor();
        }
    }

    public void OnReleased()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 2f;
        isFalling = true;
        // Clamp ball within screen by physics (use edge colliders in scene)
    }

    // Collision-based stacking removed. TubeManager will use overlap checks instead.

    public void PlayClearEffect()
    {
        if (clearEffect != null)
        {
            // Detach the particle effect so it persists after the ball is destroyed
            clearEffect.transform.SetParent(null);
            clearEffect.Play();
            Destroy(clearEffect.gameObject, clearEffect.main.duration);
        }

        // Play DOTween scale animation before destroying the ball
        transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => Destroy(gameObject));
    }
}