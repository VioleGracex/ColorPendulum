using UnityEngine;

public class Ball : MonoBehaviour
{
    public BallColor color;
    public SpriteRenderer spriteRenderer;
    // public Sprite[] colorSprites; // No longer needed
    public ParticleSystem clearEffect;

    private Rigidbody2D rb;
    private bool isFalling = false;

    public void SetColor(BallColor c)
    {
        color = c;
        if (spriteRenderer != null)
        {
            Color[] colors = { Color.red, Color.green, Color.blue };
            int idx = (int)color;
            if (idx >= 0 && idx < colors.Length)
                spriteRenderer.color = colors[idx];
            else
                spriteRenderer.color = Color.white;
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
            clearEffect.Play();
            Destroy(gameObject, clearEffect.main.duration);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}