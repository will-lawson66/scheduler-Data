using System.Windows;
using System.Windows.Controls;

namespace Instrument.Data.UI.Controls
{
    /// <summary>
    /// Interaction logic for FormFieldControl.xaml
    /// </summary>
    public partial class FormFieldControl : UserControl
    {
        public FormFieldControl()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(FormFieldControl), new PropertyMetadata(string.Empty));

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(nameof(Content), typeof(object), typeof(FormFieldControl), new PropertyMetadata(null));

        public new object Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public static readonly DependencyProperty HelperTextProperty =
            DependencyProperty.Register(nameof(HelperText), typeof(string), typeof(FormFieldControl), new PropertyMetadata(string.Empty, OnHelperTextChanged));

        public string HelperText
        {
            get { return (string)GetValue(HelperTextProperty); }
            set { SetValue(HelperTextProperty, value); }
        }

        public static readonly DependencyProperty ErrorTextProperty =
            DependencyProperty.Register(nameof(ErrorText), typeof(string), typeof(FormFieldControl), new PropertyMetadata(string.Empty, OnErrorTextChanged));

        public string ErrorText
        {
            get { return (string)GetValue(ErrorTextProperty); }
            set { SetValue(ErrorTextProperty, value); }
        }

        public static readonly DependencyProperty HasHelperTextProperty =
            DependencyProperty.Register(nameof(HasHelperText), typeof(bool), typeof(FormFieldControl), new PropertyMetadata(false));

        public bool HasHelperText
        {
            get { return (bool)GetValue(HasHelperTextProperty); }
            private set { SetValue(HasHelperTextProperty, value); }
        }

        public static readonly DependencyProperty HasErrorProperty =
            DependencyProperty.Register(nameof(HasError), typeof(bool), typeof(FormFieldControl), new PropertyMetadata(false));

        public bool HasError
        {
            get { return (bool)GetValue(HasErrorProperty); }
            private set { SetValue(HasErrorProperty, value); }
        }

        #endregion

        private static void OnHelperTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FormFieldControl control)
            {
                control.HasHelperText = !string.IsNullOrEmpty((string)e.NewValue);
                
                // If we have helper text, clear any error
                if (control.HasHelperText)
                {
                    control.HasError = false;
                }
            }
        }

        private static void OnErrorTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FormFieldControl control)
            {
                control.HasError = !string.IsNullOrEmpty((string)e.NewValue);
                
                // If we have an error, hide helper text
                if (control.HasError)
                {
                    control.HasHelperText = false;
                }
            }
        }
    }
}
