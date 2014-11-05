Question 1: can't  call the applyTemplate() function at the mult

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


 ///
