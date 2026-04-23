using DG.Tweening;
using UnityEngine;

namespace SGUnitySDK.Initialization
{
    /// <summary>
    /// Animates a UI transform using an infinite shrink-grow loop.
    /// The current local scale is captured on Start and used as the base scale.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LoaderScaleLoop : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Min(0.01f)]
        private float _halfCycleDuration = 0.8f;

        [SerializeField, Range(0.05f, 0.99f)]
        private float _shrinkMultiplier = 0.82f;

        [SerializeField]
        private AnimationCurve _scaleCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField]
        private bool _ignoreTimeScale = true;

        #endregion

        #region Runtime State

        private Transform _targetTransform;
        private Tween _scaleTween;
        private Vector3 _startScale = Vector3.one;
        private bool _hasStarted;

        #endregion

        /// <summary>
        /// Caches the animated transform.
        /// </summary>
        private void Awake()
        {
            RectTransform rectTransform = transform as RectTransform;
            _targetTransform = rectTransform != null ? rectTransform : transform;
        }

        /// <summary>
        /// Starts the shrink-grow loop for the first time.
        /// </summary>
        private void Start()
        {
            _startScale = _targetTransform.localScale;
            _hasStarted = true;
            PlayLoop();
        }

        /// <summary>
        /// Restarts the loop when the component is re-enabled.
        /// </summary>
        private void OnEnable()
        {
            if (_hasStarted)
            {
                PlayLoop();
            }
        }

        /// <summary>
        /// Stops the loop while the component is disabled.
        /// </summary>
        private void OnDisable()
        {
            StopLoop();
        }

        /// <summary>
        /// Releases tween resources when the object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            StopLoop();
        }

        /// <summary>
        /// Validates serialized values to keep tween settings stable.
        /// </summary>
        private void OnValidate()
        {
            _halfCycleDuration = Mathf.Max(0.01f, _halfCycleDuration);
            _shrinkMultiplier = Mathf.Clamp(_shrinkMultiplier, 0.05f, 0.99f);

            if (_scaleCurve == null || _scaleCurve.length == 0)
            {
                _scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }
        }

        /// <summary>
        /// Plays an infinite yoyo tween that executes shrink-grow-shrink.
        /// </summary>
        private void PlayLoop()
        {
            StopLoop();

            _targetTransform.localScale = _startScale;

            float duration = Mathf.Max(0.01f, _halfCycleDuration);
            float multiplier = Mathf.Clamp(_shrinkMultiplier, 0.05f, 0.99f);
            Vector3 minScale = _startScale * multiplier;
            AnimationCurve scaleCurve = GetScaleCurve();

            _scaleTween = _targetTransform
                .DOScale(minScale, duration)
                .SetEase(scaleCurve)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(_ignoreTimeScale);
        }

        /// <summary>
        /// Provides the curve used by the tween, guaranteeing a valid fallback.
        /// </summary>
        /// <returns>
        /// A valid animation curve for scale interpolation.
        /// </returns>
        private AnimationCurve GetScaleCurve()
        {
            if (_scaleCurve == null || _scaleCurve.length == 0)
            {
                return AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }

            return _scaleCurve;
        }

        /// <summary>
        /// Kills the active tween, if one exists.
        /// </summary>
        private void StopLoop()
        {
            if (_scaleTween == null)
            {
                return;
            }

            if (_scaleTween.IsActive())
            {
                _scaleTween.Kill();
            }

            _scaleTween = null;
        }
    }
}
