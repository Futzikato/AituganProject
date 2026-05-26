using UnityEngine;
using Aitugan.Core;
using Aitugan.Player;

namespace Aitugan.Enemies
{
    public enum EnemyKind { Basic, Shielded, Mounted, Scout, Champion, Sleeper }

    public class EnemyBase : MonoBehaviour
    {
        public static readonly System.Collections.Generic.List<EnemyBase> AllAlive = new();

        public EnemyKind kind = EnemyKind.Basic;
        public int hp = 1;
        public float speed = 1.6f;
        public float contactDamage = 1;
        public float attackCooldown = 1.0f;
        public bool shielded = false;
        public bool sleeping = false;
        public bool alerted = false;

        public System.Action OnDeath;

        protected SpriteRenderer _sr;
        protected Rigidbody2D _rb;
        protected BoxCollider2D _col;
        float _attackTimer;
        float _knockTimer;
        Vector2 _knockVel;

        protected virtual void Awake()
        {
            AllAlive.Add(this);
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = 25;
            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0;
            _rb.freezeRotation = true;
            _col = gameObject.AddComponent<BoxCollider2D>();
            _col.size = new Vector2(0.55f, 0.7f);
            _col.offset = new Vector2(0, 0.35f);
            _col.isTrigger = true;
        }

        public void Setup(EnemyKind k)
        {
            kind = k;
            // Authored PNGs at PPU 100 are tiny in world; scale them up so a
            // basic enemy is ~1u tall, matching Aitugan.
            float baseScale = 3f;

            Sprite small = Art.Enemy != null ? Art.Enemy : ProcGfx.MakeDzungar(ProcGfx.Hex("#3A2A1C"));
            Sprite big   = Art.BigEnemy != null ? Art.BigEnemy : ProcGfx.MakeDzungar(ProcGfx.Hex("#5A1818"));

            switch (k)
            {
                case EnemyKind.Basic:
                    hp = 1; speed = 1.8f; shielded = false;
                    _sr.sprite = small;
                    transform.localScale = Vector3.one * baseScale;
                    break;
                case EnemyKind.Shielded:
                    hp = 1; speed = 1.3f; shielded = true;
                    _sr.sprite = small;
                    // tint to suggest dark lacquered shield
                    _sr.color = new Color(0.85f, 0.85f, 0.95f);
                    transform.localScale = Vector3.one * baseScale;
                    break;
                case EnemyKind.Mounted:
                    hp = 2; speed = 3.2f; shielded = false;
                    _sr.sprite = big;
                    transform.localScale = Vector3.one * (baseScale * 1.1f);
                    break;
                case EnemyKind.Scout:
                    hp = 1; speed = 1.4f; shielded = false;
                    _sr.sprite = small;
                    _sr.color = new Color(0.85f, 0.95f, 0.85f);
                    transform.localScale = Vector3.one * baseScale;
                    break;
                case EnemyKind.Champion:
                    hp = 4; speed = 2.2f; shielded = false; contactDamage = 1;
                    _sr.sprite = big;
                    transform.localScale = Vector3.one * (baseScale * 1.3f);
                    break;
                case EnemyKind.Sleeper:
                    hp = 1; speed = 0f; shielded = false; sleeping = true;
                    _sr.sprite = small;
                    _sr.color = new Color(0.6f, 0.6f, 0.85f);
                    transform.localScale = Vector3.one * baseScale;
                    break;
            }
        }

        protected virtual void Update()
        {
            if (_knockTimer > 0)
            {
                _knockTimer -= Time.deltaTime;
                _rb.linearVelocity = _knockVel;
                return;
            }
            if (sleeping) { _rb.linearVelocity = Vector2.zero; return; }

            var p = AituganController.I;
            if (p == null) return;

            Vector2 to = (Vector2)(p.transform.position - transform.position);
            float dist = to.magnitude;
            Vector2 dir = dist > 0.01f ? to / dist : Vector2.zero;

            // Scout patrols horizontally until alerted
            if (kind == EnemyKind.Scout && !alerted)
            {
                _rb.linearVelocity = new Vector2(Mathf.Sin(Time.time + GetInstanceID() * 0.1f) * 0.6f, 0);
                if (dist < 2.2f) alerted = true;
                return;
            }

            if (dist > 0.6f)
                _rb.linearVelocity = dir * speed;
            else
            {
                _rb.linearVelocity = Vector2.zero;
                _attackTimer -= Time.deltaTime;
                if (_attackTimer <= 0)
                {
                    p.TakeDamage((int)contactDamage, transform.position);
                    _attackTimer = attackCooldown;
                }
            }
        }

        public void Knockback(Vector2 vel, float duration)
        {
            _knockVel = vel;
            _knockTimer = duration;
        }

        public void Damage(int amount)
        {
            hp -= amount;
            StartCoroutine(Flash());
            if (hp <= 0) Die();
        }

        System.Collections.IEnumerator Flash()
        {
            var c = _sr.color;
            _sr.color = Color.red;
            yield return new WaitForSeconds(0.08f);
            _sr.color = c;
        }

        protected void Die()
        {
            AllAlive.Remove(this);
            OnDeath?.Invoke();
            Destroy(gameObject);
        }

        void OnDestroy() { AllAlive.Remove(this); }

        public void WakeIfAsleep()
        {
            if (sleeping) { sleeping = false; alerted = true; _sr.color = Color.white; }
        }
    }
}
