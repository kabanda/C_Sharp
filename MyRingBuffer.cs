using System.Collections.Generic;
using System.Windows;

namespace ns_main
{

        public partial class MainWindow : Window
        {
            
            
            public class MyRingBuffer
            {
                
                
                private List<double> buffer = new List<double>();

                private uint depth;
                private int pointer;
                

                public MyRingBuffer(uint aDepth)
                {
                    depth = aDepth;
                    pointer = 0;
                    for(int i=0;i<aDepth;i++)
                    {
                        buffer.Add(0 ); 
                    }
                }

                public double addNew(double num)
                {
                    buffer[pointer] = num;
                    incrementPointer();
                    double result = 0 ;
                    for (int i = 0; i < depth;i++)
                    {
                        double current = buffer[pointer];
                        result += buffer[pointer];
                        incrementPointer();
                    }

                    return result/depth;
                }



                public void incrementPointer()
                {
                    pointer++;
                    if (pointer >= depth)
                        pointer = 0;
                }
            }
         }
   
}
