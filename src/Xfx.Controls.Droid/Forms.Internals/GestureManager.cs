﻿using System;
using System.ComponentModel;
using System.Linq;
using Android.Support.V4.View;
using Android.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using View = Xamarin.Forms.View;

namespace Xfx.Controls.Droid.Forms.Internals
{
    internal class GestureManager : IDisposable
    {
        IVisualElementRenderer _renderer;
        readonly Lazy<GestureDetector> _tapAndPanDetector;
        readonly Lazy<ScaleGestureDetector> _scaleDetector;

        bool _disposed;
        bool _inputTransparent;
        bool _isEnabled;

        VisualElement Element => _renderer?.Element;

        View View => _renderer?.Element as View;

        global::Android.Views.View Control => _renderer?.View;

        public GestureManager(IVisualElementRenderer renderer)
        {
            _renderer = renderer;
            _renderer.ElementChanged += OnElementChanged;

            _tapAndPanDetector = new Lazy<GestureDetector>(InitializeTapAndPanDetector);
            _scaleDetector = new Lazy<ScaleGestureDetector>(InitializeScaleDetector);
        }

        public bool OnTouchEvent(MotionEvent e)
        {
            if (Control == null)
            {
                return false;
            }

            if (!_isEnabled || _inputTransparent)
            {
                return false;
            }

            if (!DetectorsValid())
            {
                return false;
            }

            var eventConsumed = false;
            if (ViewHasPinchGestures())
            {
                eventConsumed = _scaleDetector.Value.OnTouchEvent(e);
            }

            eventConsumed = _tapAndPanDetector.Value.OnTouchEvent(e) || eventConsumed;

            return eventConsumed;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool DetectorsValid()
        {
            // Make sure we're not testing for gestures on old motion events after our 
            // detectors have already been disposed

            if (_scaleDetector.IsValueCreated && _scaleDetector.Value.Handle == IntPtr.Zero)
            {
                return false;
            }

            if (_tapAndPanDetector.IsValueCreated && _tapAndPanDetector.Value.Handle == IntPtr.Zero)
            {
                return false;
            }

            return true;
        }

        GestureDetector InitializeTapAndPanDetector()
        {
            var context = Control.Context;
            var listener = new InnerGestureListener(new TapGestureHandler(() => View),
                new PanGestureHandler(() => View, context.FromPixels));

            return new GestureDetector(context, listener);
        }

        ScaleGestureDetector InitializeScaleDetector()
        {
            var context = Control.Context;
            var listener = new InnerScaleListener(new PinchGestureHandler(() => View), context.FromPixels);
            var detector = new ScaleGestureDetector(context, listener, Control.Handler);
            ScaleGestureDetectorCompat.SetQuickScaleEnabled(detector, true);

            return detector;
        }

        bool ViewHasPinchGestures()
        {
            return View != null && View.GestureRecognizers.OfType<PinchGestureRecognizer>().Any();
        }

        void OnElementChanged(object sender, VisualElementChangedEventArgs e)
        {
            if (e.OldElement != null)
            {
                e.OldElement.PropertyChanged -= OnElementPropertyChanged;
            }

            if (e.NewElement != null)
            {
                e.NewElement.PropertyChanged += OnElementPropertyChanged;
            }

            UpdateInputTransparent();
            UpdateIsEnabled();
        }

        void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == VisualElement.InputTransparentProperty.PropertyName)
                UpdateInputTransparent();
            else if (e.PropertyName == VisualElement.IsEnabledProperty.PropertyName)
                UpdateIsEnabled();
        }

        protected void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (disposing)
            {
                if (Element != null)
                {
                    Element.PropertyChanged -= OnElementPropertyChanged;
                }

                _renderer = null;
            }
        }

        void UpdateInputTransparent()
        {
            if (Element == null)
            {
                return;
            }

            _inputTransparent = Element.InputTransparent;
        }

        void UpdateIsEnabled()
        {
            if (Element == null)
            {
                return;
            }

            _isEnabled = Element.IsEnabled;
        }
    }
}