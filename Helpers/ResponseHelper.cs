using System.Text;
using MantoProxy.Enums;

namespace MantoProxy.Helpers
{
    class ResponseHelper
    {
        public static byte[] HandleResponse(ResponseCodes code)
        {
            switch (code)
            {
                case ResponseCodes.NotAcceptable:
                    return NotAcceptable();
                case ResponseCodes.ProxyAuthenticationRequired:
                    return ProxyAuthenticationRequired();
                case ResponseCodes.ImATeapot:
                    return ImATeapot();
                case ResponseCodes.PreconditionRequired:
                    return PreconditionRequired();
                case ResponseCodes.BadGateway:
                    return BadGateway();
                case ResponseCodes.InternalServerError:
                default:
                    return InternalServerError();
            }
        }

        private static byte[] NotAcceptable()
        {
            string response =
                "HTTP/1.1 406 Not Acceptable Required\r\n" +
                "Content-Length: 0\r\n\r\n";

            byte[] responseBytes = Encoding.ASCII.GetBytes(response);
            return responseBytes;
        }

        private static byte[] ProxyAuthenticationRequired()
        {
            string response =
                "HTTP/1.1 407 Proxy Authentication Required\r\n" +
                "Proxy-Authenticate: Basic realm=\"MantoProxy\"\r\n" +
                "Content-Length: 0\r\n\r\n";

            byte[] responseBytes = Encoding.ASCII.GetBytes(response);

            return responseBytes;
        }

        private static byte[] ImATeapot()
        {
            string response =
                "HTTP/1.1 418 I'm a teapot\r\n" +
                "Content-Length: 0\r\n\r\n";

            byte[] responseBytes = Encoding.ASCII.GetBytes(response);

            return responseBytes;
        }

        private static byte[] PreconditionRequired()
        {
            string response =
                "HTTP/1.1 428 Precondition Required\r\n" +
                "Content-Length: 0\r\n\r\n";

            byte[] responseBytes = Encoding.ASCII.GetBytes(response);
            return responseBytes;
        }

        private static byte[] InternalServerError()
        {
            string response =
                "HTTP/1.1 500 Internal Server Error\r\n" +
                "Content-Length: 0\r\n\r\n";

            byte[] responseBytes = Encoding.ASCII.GetBytes(response);

            return responseBytes;
        }

        private static byte[] BadGateway()
        {
            string response =
                "HTTP/1.1 502 Bad Gateway\r\n" +
                "Content-Length: 0\r\n\r\n";

            byte[] responseBytes = Encoding.ASCII.GetBytes(response);

            return responseBytes;
        }
    }
}