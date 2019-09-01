using SOAP_Middleware;
using System.ServiceModel.Channels;

/* You may notice that the other middleware components (MVC, static files, etc.) all have custom extension methods to make adding them easy. 
 * Let’s add an extension method for our custom middleware, too. 
 * The Microsoft.AspNetCore.Builder namespace is used, so that IApplicationBuilder users can easily call the method) */

namespace Microsoft.AspNetCore.Builder
{
    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class SOAPEndpointExtensions
    {
        public static IApplicationBuilder UseSOAPEndpoint<T>(this IApplicationBuilder builder, string path, MessageEncoder encoder)
        {
            /* ASP.NET Core middleware (custom or otherwise) can be added to an application’s pipeline 
             * with the IApplicationBuilder.UseMiddleware<T> extension method. */
            return builder.UseMiddleware<SOAPEndpointMiddleware>(typeof(T), path, encoder);

        }

        /* Because MessageEncoder is an abstract class without any implementations publicly exposed, 
         * users of this library will have to either implement their own encoders or (more likely) extract an encoder from a WCF binding. 
         * To make that easier, let’s also add a UseSOAPEndpoint overload that takes a binding (and extracts the encoder on the user’s behalf): */
        public static IApplicationBuilder UseSOAPEndpoint<T>(this IApplicationBuilder builder, string path, Binding binding)
        {
            var encoder = binding.CreateBindingElements().Find<MessageEncodingBindingElement>()?.CreateMessageEncoderFactory().Encoder;
            return builder.UseMiddleware<SOAPEndpointMiddleware>(typeof(T), path, encoder);
        }
    }
}