using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SrcDestViceVerse
{
    public class Invoker
    {
        
        private MainWindow owner;

        public Invoker(MainWindow wOwner)
        {
            owner = wOwner;
        }

        public void Invoke(string sArg)
        {
            owner.Dispatcher.Invoke(()=>
            {
                ExecuteCommand(sArg);
            });
        }

        private void ExecuteCommand(string sArg)
        {
            owner.ExecuteCommand(sArg);
        }
    }

    public class Pipeserver
    {
       
        public static Invoker ownerInvoker;
        public static string pipeName;
        private static NamedPipeServerStream pipeServer;
        private static readonly int BufferSize = 256;

        
        public static void createPipeServer()
        {
            Decoder decoder = Encoding.Default.GetDecoder();
            Byte[] bytes = new Byte[BufferSize];
            char[] chars = new char[BufferSize];
            int numBytes = 0;
            StringBuilder msg = new StringBuilder();

            pipeName = "SrcDst";
            try
            {
                pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In, 1,
                                                       PipeTransmissionMode.Message,
                                                       PipeOptions.Asynchronous);
                while (true)
                {
                    pipeServer.WaitForConnection();
                    do
                    {
                        msg.Length = 0;
                        do
                        {
                            numBytes = pipeServer.Read(bytes, 0, BufferSize);
                            if (numBytes > 0)
                            {
                                int numChars = decoder.GetCharCount(bytes, 0, numBytes);
                                decoder.GetChars(bytes, 0, numBytes, chars, 0, false);
                                msg.Append(chars, 0, numChars);
                            }
                        } while (numBytes > 0 && !pipeServer.IsMessageComplete);
                        decoder.Reset();
                        if (numBytes > 0)
                        {
                            ownerInvoker.Invoke(msg.ToString());
                        }
                    } while (numBytes != 0);
                    pipeServer.Disconnect();
                }
            }
            catch (Exception ex)
            {
                //throw new Exception("Failed to create pipeServer! the detailed messages are: " + ex.Message);
                //MessageBox.Show(ex.Message);
            }
        }

        internal static void Close()
        {
            if (pipeServer == null)
                return;
            if (pipeServer.IsConnected)
                pipeServer.Disconnect();
            pipeServer.Close();
        }
    }
}
