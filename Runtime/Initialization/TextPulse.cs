using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SGUnitySDK
{
    public class TextPulse : MonoBehaviour
    {
        public float speed = 1f;
        public float amount = 1f;
        public float duration;
        public Color _color = Color.white;

        private TextMeshProUGUI _textMeshPro;
        private Tween _pulseTween;
        private Sequence _colorSequence;

        void Awake()
        {
            _textMeshPro = GetComponent<TextMeshProUGUI>();
        }

        void OnEnable()
        {
            StartPulseSequence();
            StartColorSequence();
        }

        void OnDisable()
        {
            _pulseTween?.Kill();
            _colorSequence?.Kill();
        }

        void StartPulseSequence()
        {
            _pulseTween = _textMeshPro.transform.DOScale(Vector3.one * amount, duration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }

        void StartColorSequence()
        {
            var initialColor = _textMeshPro.color;

            _colorSequence = DOTween.Sequence();
            _colorSequence.Append(_textMeshPro.transform.DOScale(amount, duration / 2).SetEase(Ease.OutQuad));
            _colorSequence.Join(_textMeshPro.DOColor(_color, duration / 2));
            _colorSequence.Append(_textMeshPro.transform.DOScale(1f, duration / 2).SetEase(Ease.InQuad));
            _colorSequence.Join(_textMeshPro.DOColor(initialColor, duration / 2));
            _colorSequence.SetUpdate(true);
            _colorSequence.SetLoops(-1);
        }
    }
}
