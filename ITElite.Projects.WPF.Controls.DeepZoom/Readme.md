## Question 1: can't  call the applyTemplate() function at the mult

https://stackoverflow.com/questions/2939787/onapplytemplate-not-called-in-custom-control

The other two answers are correct...but not complete.
 According to this post (and my experience of just resolving this issue) 
 there are 4 things you need to check: 
 (for some reason the code blocks in this post wouldn't stay formatted if I used numbers or dashes...so letters it is)

A. The controls template and styles should be located in the Generic.xaml file a folder called Themes of the root of your project.

B. Make sure your namespaces are correct in the Generic.xaml

C. Set the style key in the constructor of your control. It is also widely recommended you put the following in a static constructor.

 static YourControl()
 {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(YourControl), new FrameworkPropertyMetadata(typeof(YourControl)));
 }
D. Ensure the following is in your assemblyinfo.cs

 [assembly: ThemeInfo(ResourceDictionaryLocation.None, 
 //where theme specific resource dictionaries are located
 //(used if a resource is not found in the     
 // or application resource dictionaries)
 ResourceDictionaryLocation.SourceAssembly 
 //where the generic resource dictionary is located
 //(used if a resource is not found in the page,
 // app, or any theme specific resource dictionaries)
 )]


## Question 2.AdornerLayer  add Custom Grid Can't be found

Solve the problem using a visual Collection and a ContentPresenter. More Detail Please reference to:
WPF Tutorial - Using A Visual Collection  http://tech.pro/tutorial/856/wpf-tutorial-using-a-visual-collection




## Question 3.The Adorner is Shown at right and bottom

Answer:ReArrange the control size as follow:

 protected override Size ArrangeOverride(Size finalSize)
        {

            _child.Arrange(new Rect(new Point(((FrameworkElement)AdornedElement).ActualWidth - finalSize.Width,
                ((FrameworkElement)AdornedElement).ActualHeight - finalSize.Height), finalSize));

            return new Size(_child.ActualWidth, _child.ActualHeight);
        }


## Question 4.The default value type does not match the type of the property
Reference:https://stackoverflow.com/questions/20398751/the-default-value-type-does-not-match-the-type-of-the-property

Give dependencyProperty default value as follow:

public static readonly DependencyProperty ToothProperty =
        DependencyProperty.Register("Tooth", typeof(Tooth), typeof(ToothUI),
                                      new PropertyMetadata(default(Tooth)));
Or simply omit setting default value for your DP:

public static readonly DependencyProperty ToothProperty =
        DependencyProperty.Register("Tooth", typeof(Tooth), typeof(ToothUI));
