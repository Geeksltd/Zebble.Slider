[logo]: https://raw.githubusercontent.com/Geeksltd/Zebble.Slider/master/Shared/Icon.png "Zebble.Slider"


## Zebble.Slider

![logo]

A Zebble plugin that allow you to select range of values.


[![NuGet](https://img.shields.io/nuget/v/Zebble.Slider.svg?label=NuGet)](https://www.nuget.org/packages/Zebble.Slider/)

> It allows the user to select a number or a range between a range.

<br>


### Setup
* Available on NuGet: [https://www.nuget.org/packages/Zebble.Slider/](https://www.nuget.org/packages/Zebble.Slider/)
* Install in your platform client projects.
* Available for iOS, Android and UWP.
<br>


### Api Usage

#### Basic usage:
```xml
<Slider Id="MySlider" MaxValue="100" MinValue="0" Step="5"></Slider>
<Slider Id="MySlider" IsRange="true" LowValue="10" MaxValue="100" MinValue="0" Step="5" UpValue="50"></Slider>
new Slider { Id = "MySlider", MinValue = 0, MaxValue = 100, IsRange = true, LowValue = 10, UpValue = 50, Step = 5 };
```

You can specify all these parameters for normal slider:

`MinValue`: The minimum value of the slider.<br>
`MaxValue`: The maximum value of the slider.<br>
`Step`: Determines the size or amount of each interval or step the slider takes between the MinValue and MaxValue. <br>
`Value`: the selected value in Slider.<br>

And if you like to use range slider you can set `IsRange = true` and specify these parameters:

`LowValue`: the left side boundary of selected range.<br>
`UpValue`: the right side boundary of selected range.

#### Actions:
In the event of changing the slider's handle position by user, you can handle it using the following:
```csharp
MySlider.ValueChanged = ValueChanged;
private void ValueChanged() { /*Do something here.*/ }
```

### Properties
| Property     | Type         | Android | iOS | Windows |
| :----------- | :----------- | :------ | :-- | :------ |
| IsRange            | bool           | x       | x   | x       |
| CaptionText            | Func<double, string>           | x       | x   | x       |
| Value            | double           | x       | x   | x       |
| LowValue            | double           | x       | x   | x       |
| UpValue            | double           | x       | x   | x       |
| MinValue            | double           | x       | x   | x       |
| MaxValue            | double           | x       | x   | x       |
| Step            | double           | x       | x   | x       |


### Events
| Event             | Type                                          | Android | iOS | Windows |
| :-----------      | :-----------                                  | :------ | :-- | :------ |
| ValueChanged               | AsyncEvent    | x       | x   | x       |
| LowValueChanged               | AsyncEvent    | x       | x   | x       |
| UpValueChanged               | AsyncEvent    | x       | x   | x       |
