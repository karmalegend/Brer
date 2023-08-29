namespace MVCListenerHostedService
{
    public class TestInjection : ITestInjection
    {
        public string TestInjectionString()
        {
            return "hello world";
        }
    }
}