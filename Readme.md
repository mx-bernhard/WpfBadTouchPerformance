### Demo application that demonstrates a massive performance issue in WPF in combination with multi-touch in deep logical/visual trees

This little project demonstrates a "suboptimal" calculation within [SetCacheFlagInAncestry] within the WPF part of .NET Framework.
To try out, you have to use a real touch device that supports multi-touch. In the only visible ListBox, try to use two fingers to pan the content around. What you will see, is a high CPU on at least one core for several minutes.
Note that more than one finger is required to expose the problematic recursive algorithm. If you use only one finger, everything will work as usual, that includes panning.
The problem lies within the method [SetCacheFlagInAncestry] in ReverseInheritProperty:
```csharp
private void SetCacheFlagInAncestry(DependencyObject element, bool newValue, DeferredElementTreeState treeState, bool shortCircuit, bool setOriginCacheFlag) { .. }
```

which contains two recursive sub-calls, one for the logical parent of `element` and one for the "visual" parent, which is a combination of various parent properties for the class hierarchies spanned by `Visual`, `Visual3d` and `ContentElement`.

Usually these two sub-calls are not a problem under one of the following conditions:
* `shortCircuit == true`
* the logical/visual tree is shallow

But as soon as a second finger is used, `shortCircuit` will be `false`. If this is combined with a bigger WPF application, then ScrollViewers deep down the tree will trigger an exponentially expensive calculation of the [TouchesOverProperty] for the element and all its ancestors on all paths back to the root element, usually a Window
 instance.

This can be observed on a Windows 10 machine with anniversary or creators update and all other updates at the date of this writing. .NET 4.7 is installed as well. But it is likely it affects earlier systems as well, although I haven't tried.


[ReverseInheritProperty]: https://referencesource.microsoft.com/#PresentationCore/Core/CSharp/System/Windows/ReverseInheritProperty.cs,7773a22f441fd041
[SetCacheFlagInAncestry]: https://referencesource.microsoft.com/#PresentationCore/Core/CSharp/System/Windows/ReverseInheritProperty.cs,2daf812c19cdbd15
[TouchesOverProperty]: https://referencesource.microsoft.com/#PresentationCore/Core/CSharp/System/Windows/Input/TouchesOverProperty.cs