using UnityEngine;

namespace CatTalk2D.Effects
{
    /// <summary>
    /// 하트 이펙트 (위로 떠오르며 사라짐)
    /// </summary>
    public class HeartEffect : MonoBehaviour
    {
        [Header("설정")]
        [SerializeField] private float _floatSpeed = 1f;
        [SerializeField] private float _lifetime = 2f;
        [SerializeField] private float _fadeStartTime = 1f;

        private SpriteRenderer _spriteRenderer;
        private float _spawnTime;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _spawnTime = Time.time;
        }

        private void Update()
        {
            // 위로 떠오르기
            transform.position += Vector3.up * _floatSpeed * Time.deltaTime;

            // 페이드 아웃
            float elapsed = Time.time - _spawnTime;
            if (elapsed > _fadeStartTime)
            {
                float alpha = 1f - ((elapsed - _fadeStartTime) / (_lifetime - _fadeStartTime));
                Color color = _spriteRenderer.color;
                color.a = alpha;
                _spriteRenderer.color = color;
            }

            // 수명 다하면 삭제
            if (elapsed >= _lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
