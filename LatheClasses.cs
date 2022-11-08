//using ImageClassLibrary;

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using Point = System.Drawing.Point;

namespace ns_main
{
    public partial class MainWindow : Window
    {
        public class LatheLineSegment
        {
            public Point SegmentStepperStartX; //{ get; set; }
            public Point StepperStart; //{ get; set; }
            public uint WoodWidth;
            public uint LatheWidth;
            private uint xReff;
            private uint contactedDistance;
            public uint line;
            private List<Point> DepthList;
            private uint Max_Y;
            

            public LatheLineSegment(uint cuttingApproachDistanceX,uint contactedDistance)
            {
                StepperStart = new Point();
                SegmentStepperStartX = new Point();
                WoodWidth = 0;
                xReff = cuttingApproachDistanceX;
                this.contactedDistance = contactedDistance;
            }

            public LatheLineSegment(uint aLine, uint from,uint cuttingApproachDistanceX,uint theContactedDistance,uint aMax_Y,List<Point> aDepthList)
            {
                StepperStart = new Point();
                SegmentStepperStartX = new Point();
                contactedDistance = theContactedDistance;
                line = aLine;
                SegmentStepperStartX.X = (int) from;
                SegmentStepperStartX.Y = (int) aLine;
                WoodWidth = 0;
                StepperStart.X = (int) getStepperXStart();
                StepperStart.Y = (int) getStepperY();
                xReff = cuttingApproachDistanceX;
                
                DepthList = aDepthList;
                Max_Y = aMax_Y;
                
            }

            public LatheLineSegment Copy()
            {
                  return new LatheLineSegment(line, (uint) StepperStart.X, xReff, contactedDistance, Max_Y, DepthList);
            }

            /*public List<LatheLineSegment> getLineValleysMouths(uint line)
            {
                List<LatheLineSegment> segments = new List<LatheLineSegment>();
                List<LatheLineSegment> valleys = new List<LatheLineSegment>();
                LatheLineSegment nextValley = new LatheLineSegment(line,(uint) start.X,xReff,yReff,Max_Y,DepthList);
                long xIncrement;
                for(long x = 0; x<DepthList.Count;x +=xIncrement)
                {
                    if (isValleyMouthStart(line,(uint) x))
                    {
                        nextValley = getValleyWidth(line, (uint) x);
                        valleys.Add(nextValley);
                        xIncrement =  (nextValley.width == 0)?1:nextValley.width;
                    }
                    else
                    {
                        xIncrement = 1;
                    }
                }
                return valleys;
            }
            
            public LatheLineSegment getValleyWidth(uint line, uint from)
            {
                LatheLineSegment lls = new LatheLineSegment(line,(uint) start.X,xReff,yReff,Max_Y,DepthList);

                lls.width = getValleyEnd(line, from);
            
                return lls;
            }*/
            
            public bool isValleyMouthStart(uint line)
            {
                if (!inRange(StepperStart.X,0,DepthList.Count-1))
                    return false;
                if (StepperStart.X == 0 && toCut((uint) StepperStart.X,line))
                    return true;

                var _start = (StepperStart.X == 0) ? 1 : StepperStart.X;
            
                if(!toCut((uint) (_start - 1), line) && toCut((uint) _start, line))
                {
                    return true;
                }
                return false;
            }
            
            public bool isValleyMouthEnd(uint line, uint from)
            {
                if (!inRange(from,1,DepthList.Count-1))
                    return false;
            
                return (toCut(from, line) && !toCut(from+1, line) ) || (from == DepthList.Count-1);           
            }
            /*public uint getValleyEnd(uint line, int Max_X)
            {
                if(toCutWholeLine(line,Max_X))
                    return UInt32.MaxValue;
            

                uint current = 0;
                while (!isValleyMouthEnd(line, current+from) && (current+from) < DepthList.Count)
                {
                    current++;
                }
            
                return current;          
            }*/
            
            /// <summary>
            /// Get next valley start from  wherever you are
            /// </summary>
            /// <param name="line"></param>
            /// <param name="from"></param>
            /// <returns>index of valley start was found starting from 'from' = from + distanceToNextValleyStart
            /// Use getValleyEnd for next from</returns>
            /*public uint getValleyStart(uint line, uint from, int Max_X)
            {
                if(toCutWholeLine(line,Max_X))
                    return UInt32.MaxValue;
            
                uint current = from;
                while (!isValleyMouthStart(current, line) && current < DepthList.Count)
                {
                    current++;
                }

                if (current >= DepthList.Count)
                    return UInt32.MaxValue;
                return current;
            }*/

            public uint getStepperXStart()
            {
                 return LeftLineStepperX+MMX(SegmentStepperStartX.X/10.0);
            }
            
            public uint getStepperXEnd()
            {
                var result = (uint) (LeftLineStepperX+MMX((SegmentStepperStartX.X + WoodWidth)/10.0));
                if (result > RightLineStepperX)
                {
                    result = RightLineStepperX;
                }
                return result;
            }
            
            public uint getStepperY()
            {
                //var result = (uint)(Math.Round((double)contactedDistance + ((MMX((double)TotalLines-1.0-(double)line))/10.0)));
                // var debug = MMX(TotalLines/10);
                // var result = contactedDistance+MMX(TotalLines - line)/10.0;
                //return (uint) ((uint) Zero_Y-(MMX(line)/10.0));
                //return (uint) mapRange(line, 0, 90, Zero_Y,Contact_18 );
   
                var result = (uint) mapRange(line, 0, TotalLines, Zero_Y,Zero_Y-(MMY(line/10.0 )) );
                return result;
            }
            
            /// <summary>
            /// See if point is to be cut away i.e not part of model
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            public bool toCut(uint x, uint y)
            {

                if (!inRange(x, 0, DepthList.Count - 1))
                    return false;
            
                var point = DepthList[(int) x];
                return (y <= Max_Y && y > point.Y);
            }
            public bool toCutWholeLine(uint line,int Max_X)
            {
                return toCutInLine(line,Max_X).Count == DepthList.Count;
            }
            
            public List<int> toCutInLine(uint line,int Max_X)
            {
                return Enumerable.Range(0, Max_X).Where(cut => toCut((uint) cut,  line)).ToList();
    
            }
        }

        public class MyPointStore
        {
            private Point point;
            private Color color;
            private System.Drawing.Bitmap bitmap;
            private List<MyPointStore> surroundingPoints;
            
            public MyPointStore(Point aPoint, System.Drawing.Bitmap aBitmap)
            {
                point = aPoint;
                bitmap = aBitmap;
                color = aBitmap.GetPixel(aPoint.X,aPoint.Y);
            }
            
            public MyPointStore(Point aPoint)
            {
                point = aPoint;
                color = bitmap.GetPixel(aPoint.X,aPoint.Y);
            }

            public void storeSurrounding()
            {
                surroundingPoints = new List<MyPointStore>();
                surroundingPoints.Add(new MyPointStore( new Point(point.X,point.Y),bitmap));
                if(point.X>0)
                    surroundingPoints.Add(new MyPointStore( new Point(point.X-1, point.Y),bitmap));
                if(point.X<ModelMMWidth-1)
                    surroundingPoints.Add(new MyPointStore( new Point(point.X+1, point.Y),bitmap));
                if(point.Y<TotalLines-1)
                    surroundingPoints.Add(new MyPointStore( new Point(point.X, point.Y+1),bitmap));
                if(point.Y>0)
                    surroundingPoints.Add(new MyPointStore( new Point(point.X, point.Y-1),bitmap));
                ///////////////////////////////////////////////////////////////////////////////
                //ImageBitmap.SetPixel(x, y, woodImageBitmap.GetPixel(x, y));

    
            }

            private bool pointIsValid(Point point)
            {
                if (point.X < 0 || point.X >= ModelMMWidth)
                    return false;
                if (point.Y < 0 || point.Y >= TotalLines)
                    return false;
                return true;
            }
            public void storeSurrounding(uint range)
            {
                surroundingPoints = new List<MyPointStore>();
                
                for (var Y_offset = (range * -1) + 1; Y_offset < range; Y_offset++)
                {
                    for (var X_offset = (range * -1) + 1; X_offset < range; X_offset++)
                    {
                        var nextPoint = new Point((int) (point.X+X_offset), (int) (point.Y+Y_offset));
                        if(pointIsValid(nextPoint))
                            surroundingPoints.Add(new MyPointStore( nextPoint,bitmap));
                    }
                }


            }

            public void setSurroundingColor(Color color)
            {
                foreach (var surroundingPoint in surroundingPoints)
                {
                    bitmap.SetPixel(surroundingPoint.point.X,surroundingPoint.point.Y,color);
                }
            }
            
            public void restoreSurroundingColor()
            {
                foreach (var surroundingPoint in surroundingPoints)
                {
                    bitmap.SetPixel(surroundingPoint.point.X,surroundingPoint.point.Y,surroundingPoint.color);
                }

            }

        }
        
    }
}