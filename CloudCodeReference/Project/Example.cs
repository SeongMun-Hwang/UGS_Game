using Unity.Services.CloudCode.Core;

namespace HelloWorld;

public class MyModule
{
    //함수이름은 Hello지만 클라우드 코드에서는 SayHello라고 호출할 것이다
    [CloudCodeFunction("SayHello")]
    public string Hello(string name)
    {
        return $"Hello, {name}!";
    }
}