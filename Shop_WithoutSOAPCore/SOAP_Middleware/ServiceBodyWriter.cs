using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.Xml;

namespace SOAP_Middleware
{
    /* Finally, with a response in hand, we can use the MessageEncoder specified by the user 
     * to send the object back to the caller in the HTTP response.
     * Message.CreateMessage requires an implementation of BodyWriter to output the body of 
     * the message with correct element names.  */
    public class ServiceBodyWriter : BodyWriter
    {
        string ServiceNamespace;
        string EnvelopeName;
        string ResultName;
        object Result;

        public ServiceBodyWriter(string serviceNamespace, string envelopeName, string resultName, object result) : base(isBuffered: true)
        {
            ServiceNamespace = serviceNamespace;
            EnvelopeName = envelopeName;
            ResultName = resultName;
            Result = result;
        }

        protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
        {
            writer.WriteStartElement(EnvelopeName, ServiceNamespace);
            var serializer = new DataContractSerializer(Result.GetType(), ResultName, ServiceNamespace);
            serializer.WriteObject(writer, Result);
            writer.WriteEndElement();
        }
    }
}
