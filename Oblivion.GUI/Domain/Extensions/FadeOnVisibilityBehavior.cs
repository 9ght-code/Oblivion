using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Xaml.Behaviors;

namespace Oblivion.GUI.Domain.Extensions;

public class FadeOnVisibilityBehavior : Behavior<FrameworkElement>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.IsVisibleChanged += OnVisibilityChanged;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.IsVisibleChanged -= OnVisibilityChanged;
        base.OnDetaching();
    }

    private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            AssociatedObject.RenderTransform = new TranslateTransform(0, 10);
            var sb = new Storyboard();

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeIn, AssociatedObject);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));

            var slideUp = new DoubleAnimation(10, 0, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(slideUp, AssociatedObject);
            Storyboard.SetTargetProperty(slideUp,
                new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

            sb.Children.Add(fadeIn);
            sb.Children.Add(slideUp);
            sb.Begin();
        }
    }
}
