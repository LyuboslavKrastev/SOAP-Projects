using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;


namespace SOAP_Middleware
{
    public class SOAPEndpointMiddleware
    {
        /* ASP.NET Core middleware uses the explicit dependencies principle, so all dependencies should be provided through 
         * dependency injection via arguments to the middleware’s constructor. 
         * The one dependency common to most middleware is a RequestDelegate object representing the next delegate in 
         * the HTTP request processing pipeline. If our middleware does not completely handle a request, the request’s context 
         * should be passed along to this next delegate. */

        // The middleware delegate to call after this one finishes processing
        private readonly RequestDelegate _next;
        private readonly string _endpointPath;
        // Note that the MessageEncoder class is in the System.ServiceModel.Primitives contract.
        private readonly MessageEncoder _messageEncoder;
        private readonly ServiceDescription _service;

        /*  The middleware should listen for SOAP requests to come in to a particular endpoint (URL) and then dispatch the calls 
         *  to the appropriate service API based on the SOAP action specified. For this to work, our custom middleware will need 
         *  a few more pieces of information:
             *  The path it should listen on for requests
             *  The type of the service to invoke methods from
             *  The MessageEncoder used to encode the incoming SOAP payloads */
        public SOAPEndpointMiddleware(RequestDelegate next, Type serviceType, string path, MessageEncoder encoder)
        {
            _next = next;
            _endpointPath = path;
            _messageEncoder = encoder;
            _service = new ServiceDescription(serviceType);
        }


        /*  We need to handle incoming HTTP request contexts. 
         *  For this, middleware is expected to have an Invoke method taking an HttpContext parameter. 
         *  This method should take whatever actions are necessary based on the HttpContext being processed 
         *  and then call the next middleware in the HTTP request processing pipeline (unless no further processing is needed). */
        public async Task Invoke(HttpContext httpContext, IServiceProvider serviceProvider)
        {
            if (httpContext.Request.Path.Equals(_endpointPath, StringComparison.Ordinal))
            {

                /* If the request’s path does equal the expected path for our service endpoint, 
                 * we need to read the message and compose a response.*/
                Message responseMessage;

                // Read request message
                var requestMessage = _messageEncoder.ReadMessage(httpContext.Request.Body, 0x10000, httpContext.Request.ContentType);

                /* After that, we need to get the requested action by looking for a ‘SOAPAction’ header
                 * (which is how SOAP actions are usually communicated). */

                var soapAction = httpContext.Request.Headers["SOAPAction"].ToString().Trim('\"');
                if (!string.IsNullOrEmpty(soapAction))
                {
                    requestMessage.Headers.Action = soapAction;
                }

                // Lookup operation and invoke
                var operation = _service.Operations
                    .Where(o => o.Name.Equals(requestMessage.Headers.Action, StringComparison.Ordinal))
                    .FirstOrDefault();

                if (operation == null)
                {
                    throw new InvalidOperationException($"No operation found for specified action: {requestMessage.Headers.Action}");
                }

                // Invoking the operation

                /* Now that we have a MethodInfo to invoke, we need to extract the arguments to pass to the operation from the request’s body.
                 * This can be done in a helper method with an XmlReader and DataContractSerializer. */

                // Get service type
                var serviceInstance = serviceProvider.GetService(_service.ServiceType);

                // Get operation arguments from message
                var arguments = GetRequestArguments(requestMessage, operation);

                // Invoke Operation method
                var responseObject = operation.DispatchMethod.Invoke(serviceInstance, arguments.ToArray());
                
                // Create response message
                var resultName = operation.DispatchMethod.ReturnParameter.GetCustomAttribute<MessageParameterAttribute>()?.Name ?? operation.Name + "Result";
                var bodyWriter = new ServiceBodyWriter(operation.Contract.Namespace, operation.Name + "Response", resultName, responseObject);
                responseMessage = Message.CreateMessage(_messageEncoder.MessageVersion, operation.ReplyAction, bodyWriter);

                httpContext.Response.ContentType = httpContext.Request.ContentType; // _messageEncoder.ContentType;
                httpContext.Response.Headers["SOAPAction"] = responseMessage.Headers.Action;

                _messageEncoder.WriteMessage(responseMessage, httpContext.Response.Body);


            }
            else
            {
                await _next(httpContext);
            }
        }

        /* This argument reading helper assumes the arguments are provided in order in the message body. 
         * This is true for messages coming from .NET WCF clients, but may not be true for all SOAP clients. */
        private object[] GetRequestArguments(Message requestMessage, OperationDescription operation)
        {
            var parameters = operation.DispatchMethod.GetParameters();
            var arguments = new List<object>();

            // Deserialize request wrapper and object
            using (var xmlReader = requestMessage.GetReaderAtBodyContents())
            {
                // Find the element for the operation's data
                xmlReader.ReadStartElement(operation.Name, operation.Contract.Namespace);

                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameterName = parameters[i].GetCustomAttribute<MessageParameterAttribute>()?.Name ?? parameters[i].Name;
                    xmlReader.MoveToStartElement(parameterName, operation.Contract.Namespace);
                    if (xmlReader.IsStartElement(parameterName, operation.Contract.Namespace))
                    {
                        var serializer = new DataContractSerializer(parameters[i].ParameterType, parameterName, operation.Contract.Namespace);
                        arguments.Add(serializer.ReadObject(xmlReader, verifyObjectName: true));
                    }
                }
            }

            return arguments.ToArray();
        }
    }
}
