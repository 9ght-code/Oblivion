using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Xaml.Behaviors;

namespace Oblivion.GUI.Domain.Extensions;

public class ButtonScaleBehavior : Behavior<FrameworkElement>
{
    private ScaleTransform? _scale;

    protected override void OnAttached()
    {
        base.OnAttached();
        _scale = new ScaleTransform(1, 1);
        AssociatedObject.RenderTransformOrigin = new Point(0.5, 0.5);
        AssociatedObject.RenderTransform = _scale;
        AssociatedObject.PreviewMouseDown += OnMouseDown;
        AssociatedObject.PreviewMouseUp += OnMouseUp;
        AssociatedObject.MouseLeave += OnMouseLeave;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewMouseDown -= OnMouseDown;
        AssociatedObject.PreviewMouseUp -= OnMouseUp;
        AssociatedObject.MouseLeave -= OnMouseLeave;
        base.OnDetaching();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        AnimateScale(0.96, 80);
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        AnimateScale(1.0, 100);
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        AnimateScale(1.0, 100);
    }

    private void AnimateScale(double to, int ms)
    {
        if (_scale == null) return;

        var anim = new DoubleAnimation(to, TimeSpan.FromMilliseconds(ms))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        _scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
        _scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
    }
}
