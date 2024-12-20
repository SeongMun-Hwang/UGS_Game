using Unity.Services.CloudCode.Core;

namespace HelloWorld;

public class MyModule
{
    //�Լ��̸��� Hello���� Ŭ���� �ڵ忡���� SayHello��� ȣ���� ���̴�
    [CloudCodeFunction("SayHello")]
    public string Hello(string name)
    {
        return $"Hello, {name}!";
    }
}