namespace MantoProxy.Enums
{
    enum ResponseCodes
    {
        NotAcceptable = 406,

        ProxyAuthenticationRequired = 407,

        ImATeapot = 418,

        PreconditionRequired = 428,

        InternalServerError = 500,

        BadGateway = 502,
    }
}