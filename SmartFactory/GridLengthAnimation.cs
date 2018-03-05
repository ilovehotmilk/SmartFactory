using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace SmartFactory
{
    public class GridLengthAnimation : AnimationTimeline
    {
        static GridLengthAnimation()
        {
            FromProperty = DependencyProperty.Register("From", typeof(GridLength),
                typeof(GridLengthAnimation));

            ToProperty = DependencyProperty.Register("To", typeof(GridLength),
                typeof(GridLengthAnimation));
        }

        public static readonly DependencyProperty FromProperty;
        public GridLength From
        {
            get
            {
                return (GridLength)GetValue(GridLengthAnimation.FromProperty);
            }
            set
            {
                SetValue(GridLengthAnimation.FromProperty, value);
            }
        }

        public static readonly DependencyProperty ToProperty;
        public GridLength To
        {
            get
            {
                return (GridLength)GetValue(GridLengthAnimation.ToProperty);
            }
            set
            {
                SetValue(GridLengthAnimation.ToProperty, value);
            }
        }

        protected override System.Windows.Freezable CreateInstanceCore()
        {
            return new GridLengthAnimation();
        }

       
        public override Type TargetPropertyType
        {
            get
            {
                return typeof(GridLength);
            }
        }

        public const string EasingFunctionPropertyName = "EasingFunction";

        public IEasingFunction EasingFunction
        {
            get
            {
                return (IEasingFunction)GetValue(EasingFunctionProperty);
            }
            set
            {
                SetValue(EasingFunctionProperty, value);
            }
        }

        public static readonly DependencyProperty EasingFunctionProperty = DependencyProperty.Register(
          EasingFunctionPropertyName,
          typeof(IEasingFunction),
          typeof(GridLengthAnimation),
          new UIPropertyMetadata(null));

        public override object GetCurrentValue(object defaultOriginValue,
            object defaultDestinationValue, AnimationClock animationClock)
        {
            double fromVal = ((GridLength)GetValue(FromProperty)).Value;

            double toVal = ((GridLength)GetValue(ToProperty)).Value;

            double progress = animationClock.CurrentProgress.Value;

            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                progress = easingFunction.Ease(progress);
            }


            if (fromVal > toVal)
                return new GridLength((1 - progress) * (fromVal - toVal) + toVal, GridUnitType.Star);

            return new GridLength(progress * (toVal - fromVal) + fromVal, GridUnitType.Star);
        }
    }
}
