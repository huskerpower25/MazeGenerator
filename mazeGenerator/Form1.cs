using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mazeGenerator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public static int rows = 40;
        public static int cols = 40;
        public static int offset = 30;
        public static int h = 800;
        public static int w = 800;

        bool mazeComplete = false;

        int xLoc = 0;
        int yLoc = 0;

        public static int startX;
        public static int startY;
        public static int endX;
        public static int endY;

        PictureBox maze = new PictureBox();        

        cell[,] grid = new cell[rows, cols];

        public static List<lines> borderList = new List<lines>();

        List<cell> cellStack = new List<cell>();

        List<Pathway> allPaths = new List<Pathway>();

        public Random rand = new Random(DateTime.Now.Millisecond);

        SolidBrush visitedBrush = new SolidBrush(Color.LightGray);
        SolidBrush currentBrush = new SolidBrush(Color.CadetBlue);
        SolidBrush startBrush = new SolidBrush(Color.Green);
        SolidBrush endBrush = new SolidBrush(Color.Red);
        SolidBrush pathBrush = new SolidBrush(Color.DarkOliveGreen);

        Pen gridPen = new Pen(Color.Black, 2);
        Pen borderPen = new Pen(Color.Black, 4);

        private void updateBitmap()
        {
            maze.Size = new Size(w, h);
            maze.Location = new Point(offset, offset);
            this.Controls.Add(maze);
            Bitmap mazeBitmap = new Bitmap(w, h);
            Graphics mazeGraphics = Graphics.FromImage(mazeBitmap);

            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    foreach (var item in grid[i, j].linesCollection)
                    {
                        mazeGraphics.DrawLine(gridPen, item.x1, item.y1, item.x2, item.y2);
                    }
                    if (grid[i, j].visited)
                    {
                        mazeGraphics.FillRectangle(visitedBrush, grid[i, j].rectLoc);
                    }
                    if (grid[i, j].current)
                    {
                        mazeGraphics.FillRectangle(currentBrush, grid[i, j].rectLoc);
                    }
                    if (grid[i, j].partOfFirstPath)
                    {
                        mazeGraphics.FillRectangle(pathBrush, grid[i, j].rectLoc);
                    }
                }
            }



            //draw start and end
            mazeGraphics.FillRectangle(startBrush, grid[startX, startY].rectLoc);
            mazeGraphics.FillRectangle(endBrush, grid[endX, endY].rectLoc);

            // draw border
            foreach (var item in borderList)
            {
                mazeGraphics.DrawLine(borderPen, item.x1, item.y1, item.x2, item.y2);
            }

            maze.Image = mazeBitmap;
            this.Refresh();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Height = h + offset * 5;
            this.Width = w + offset *2;
            setup();
        }



        #region Button Events
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void btnGo_Click(object sender, EventArgs e)
        {
            if (!tmrEngine.Enabled)
            {
                tmrEngine.Start();
            }
        }
        private void btnReset_Click(object sender, EventArgs e)
        {
            setup();
        }
        private void btnInstant_Click(object sender, EventArgs e)
        {
            instantSolve();
        }
        private void btnPause_Click(object sender, EventArgs e)
        {
            tmrEngine.Stop();
        }
        private void btnSolve_Click(object sender, EventArgs e)
        {
            solveGeneticAlgorithm();
        }

        private void btnSolveAllPaths_Click(object sender, EventArgs e)
        {
            solveAllPaths();
        }
        #endregion

        #region Methods        
        private void setup()
        {
            mazeComplete = false;

            startX = 0;
            startY = 0;

            endX = cols - 1;
            endY = rows - 1;

           // h = this.Height;
           // w = this.Width;


            // make border
            lines topLine = new lines();
            lines bottomLine = new lines();
            lines leftLine = new lines();
            lines rightLine = new lines();

            int x1, x2, y1, y2;

            x1 = 0; // = offset
            y1 = 0; // = offset;
            x2 = h; // + offset;
            y2 = w; // + offset;

            topLine.x1 = x1; topLine.y1 = y1; topLine.x2 = x2; topLine.y2 = y1;
            leftLine.x1 = x1; leftLine.y1 = y1; leftLine.x2 = x1; leftLine.y2 = y2;
            rightLine.x1 = x2; rightLine.y1 = y1; rightLine.x2 = x2; rightLine.y2 = y2;
            bottomLine.x1 = x1; bottomLine.y1 = y2; bottomLine.x2 = x2; bottomLine.y2 = y2;

            borderList.Add(topLine);
            borderList.Add(leftLine);
            borderList.Add(bottomLine);
            borderList.Add(rightLine);

            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    grid[i, j] = new cell();
                    grid[i, j].i = i;
                    grid[i, j].j = j;
                    grid[i, j].create();

                }
            }
            updateBitmap();
        }
        private void erase(int i, int j, cell.wallType walltype)
        {
            for (int k = 0; k < grid[i, j].linesCollection.Count(); k++)
            {
                if (grid[i, j].linesCollection.ElementAt(k).wallType == walltype)
                {
                    grid[i, j].linesCollection.RemoveAt(k);
                    return;
                }
            }
            //grid[i, j].linesCollection.RemoveAll(walltype);

            //foreach (var item in grid[i, j].linesCollection)
            //{
            //    if (item.wallType == walltype)
            //    {
            //        grid[i, j].linesCollection.Remove(item);
            //        return;
            //    }
            //} 
        }
        private cell.pos moveTo(int x, int y)
        {
            cell.pos[] neighbors = getNeighbors(x, y);
            int count = 0;
            int index = 0;
            foreach (var item in neighbors)
            {
                if (item != cell.pos.empty)
                {
                    count++;
                }
            }
            if (count == 0)
            {
                return cell.pos.empty;
            }
            else
            {
                index = rand.Next(0, count);
            }

            Console.WriteLine($"{neighbors[0]}");
            Console.WriteLine($"{neighbors[1]}");
            Console.WriteLine($"{neighbors[2]}");
            Console.WriteLine($"{neighbors[3]}");

            return neighbors[index];
        }
        private void tmrEngine_Tick(object sender, EventArgs e)
        {
            
            cellStack.Add(grid[xLoc, yLoc]);

            if (cellStack.Count > 1)
            {
                cellStack.ElementAt(cellStack.Count - 2).current = false;
            }
            grid[xLoc, yLoc].current = true;
            grid[xLoc, yLoc].visited = true;


            cell.pos moveToPos = moveTo(xLoc, yLoc);
            Console.WriteLine();
            Console.WriteLine(moveToPos.ToString());
            Console.WriteLine($"{xLoc},{yLoc}");

            switch (moveToPos)
            {
                case cell.pos.empty:
                    if (cellStack.Count > 0)
                    {
                        while (cellStack.Count > 1 && moveTo(xLoc,yLoc) == cell.pos.empty)
                        {
                            xLoc = cellStack.ElementAt(cellStack.Count - 2).i;
                            yLoc = cellStack.ElementAt(cellStack.Count - 2).j;
                            cellStack.ElementAt(cellStack.Count - 1).current = false;
                            cellStack.RemoveAt(cellStack.Count - 1);
                        } 
                    }

                    break;
                case cell.pos.above:
                    erase(xLoc, yLoc - 1, cell.wallType.bottomWall);
                    erase(xLoc, yLoc, cell.wallType.topWall);
                    grid[xLoc, yLoc].topWall = false;
                    grid[xLoc, yLoc-1].bottomWall = false;
                    yLoc--;
                    break;
                case cell.pos.left:

                    erase(xLoc - 1, yLoc, cell.wallType.rightWall);
                    erase(xLoc, yLoc, cell.wallType.leftWall);
                    grid[xLoc-1, yLoc].rightWall = false;
                    grid[xLoc, yLoc].leftWall = false;
                    xLoc--;
                    break;
                case cell.pos.below:

                    erase(xLoc, yLoc + 1, cell.wallType.topWall);
                    erase(xLoc, yLoc, cell.wallType.bottomWall);
                    grid[xLoc, yLoc + 1].topWall = false;
                    grid[xLoc, yLoc].bottomWall = false;
                    yLoc++;
                    break;
                case cell.pos.right:

                    erase(xLoc + 1, yLoc, cell.wallType.leftWall);
                    erase(xLoc, yLoc, cell.wallType.rightWall);
                    grid[xLoc + 1, yLoc].leftWall = false;
                    grid[xLoc, yLoc].rightWall = false;
                    xLoc++;
                    break;                
                default:
                    break;
            }
            Console.WriteLine($"{xLoc},{yLoc}");
            Console.WriteLine(cellStack.Count());

            int count = 0;
            foreach (var item in grid)
            {
                if (item.visited)
                {
                    count++;
                }
            }
            if (count == rows*cols)
            {
                mazeComplete = true;
                tmrEngine.Stop();
            }
            updateBitmap();
        }
        public cell.pos[] getNeighbors(int x, int y)
        {
            cell.pos[] neighbors = new cell.pos[4];

            int count = 0;
            // check top
            if (y > 0 && grid[x, y - 1].visited == false)
            {                
                neighbors[count] = cell.pos.above;
                count++;
            }
            
            // check left
            if (x > 0 && grid[x - 1, y].visited == false)
            {
                neighbors[count] = cell.pos.left;
                count++;
            }

            // check bottom 
            if (y < rows-1 && grid[x, y + 1].visited == false)
            {
                neighbors[count] = cell.pos.below;
                count++;
            }

            // check right
            if (x < cols-1 && grid[x + 1, y].visited == false)
            {
                neighbors[count] = cell.pos.right;
            }

            return neighbors;

        }
        private void instantSolve()
        {
            setup();
            int count = 0;
            while (count < rows * cols)
            {
                cellStack.Add(grid[xLoc, yLoc]);

                if (cellStack.Count > 1)
                {
                    cellStack.ElementAt(cellStack.Count - 2).current = false;
                }
                grid[xLoc, yLoc].current = true;
                grid[xLoc, yLoc].visited = true;


                cell.pos moveToPos = moveTo(xLoc, yLoc);
                Console.WriteLine();
                Console.WriteLine(moveToPos.ToString());
                Console.WriteLine($"{xLoc},{yLoc}");

                switch (moveToPos)
                {
                    case cell.pos.empty:
                        if (cellStack.Count > 0)
                        {
                            while (cellStack.Count > 1 && moveTo(xLoc, yLoc) == cell.pos.empty)
                            {
                                xLoc = cellStack.ElementAt(cellStack.Count - 2).i;
                                yLoc = cellStack.ElementAt(cellStack.Count - 2).j;
                                cellStack.ElementAt(cellStack.Count - 1).current = false;
                                cellStack.RemoveAt(cellStack.Count - 1);
                            }
                        }

                        break;
                    case cell.pos.above:
                        erase(xLoc, yLoc - 1, cell.wallType.bottomWall);
                        erase(xLoc, yLoc, cell.wallType.topWall);
                        grid[xLoc, yLoc].topWall = false;
                        grid[xLoc, yLoc - 1].bottomWall = false;
                        yLoc--;
                        break;
                    case cell.pos.left:

                        erase(xLoc - 1, yLoc, cell.wallType.rightWall);
                        erase(xLoc, yLoc, cell.wallType.leftWall);
                        grid[xLoc - 1, yLoc].rightWall = false;
                        grid[xLoc, yLoc].leftWall = false;
                        xLoc--;
                        break;
                    case cell.pos.below:

                        erase(xLoc, yLoc + 1, cell.wallType.topWall);
                        erase(xLoc, yLoc, cell.wallType.bottomWall);
                        grid[xLoc, yLoc + 1].topWall = false;
                        grid[xLoc, yLoc].bottomWall = false;
                        yLoc++;
                        break;
                    case cell.pos.right:

                        erase(xLoc + 1, yLoc, cell.wallType.leftWall);
                        erase(xLoc, yLoc, cell.wallType.rightWall);
                        grid[xLoc + 1, yLoc].leftWall = false;
                        grid[xLoc, yLoc].rightWall = false;
                        xLoc++;
                        break;
                    default:
                        break;
                }
                Console.WriteLine($"{xLoc},{yLoc}");
                Console.WriteLine(cellStack.Count());

                count = 0;
                foreach (var item in grid)
                {
                    if (item.visited)
                    {
                        count++;
                    }
                }
            }
            mazeComplete = true;
            updateBitmap();
        }
        #endregion

        #region Finding All Paths
        private void solveAllPaths()
        {

            Pathway firstPath = new Pathway();
            int[] startPos = new int[2];
            startPos[0] = startX;
            startPos[1] = startY;

            firstPath.path.Add(startPos);

            int x = startPos[0];
            int y = startPos[1];
            grid[x, y].partOfPath = true;

            allPaths.Add(firstPath);

            moveUntilCompleteFirstSolution();

            //moveUntilCompleteAllSolutions(allPaths);
        }       
        private Pathway moveUntilCompleteFirstSolution()
        {
            Pathway firstPathway = new Pathway();
            firstPathway = allPaths.Last();
            bool pathComplete = false;

            while (!pathComplete)
            {

                int[] currentLocation = new int[2];
                int[] nextLocation = new int[2];

                currentLocation = firstPathway.path.Last();

                grid[currentLocation[0], currentLocation[1]].partOfPath = true;
                                
                if (currentLocation[0] == endX && currentLocation[1] == endY)
                {
                    pathComplete = true;
                    break;
                }

                // get all possible moves
                firstPathway.availableMoves =  getPossibleMoves_VisitedNotPossible(currentLocation);

                switch (firstPathway.availableMoves.Count())
                {
                    case 0:
                        bool possibleMoveExists = false;
                        while (!possibleMoveExists)
                        {
                            currentLocation = firstPathway.path.Last();
                            firstPathway.availableMoves = getPossibleMoves_VisitedNotPossible(currentLocation);
                            if (firstPathway.availableMoves.Count() > 0)
                            {
                                possibleMoveExists = true;
                            nextLocation = moveTo_SolveAllPaths(grid[currentLocation[0], currentLocation[1]], firstPathway.availableMoves.ElementAt(0));

                        }

                        else
                            {
                                firstPathway.path.RemoveAt(firstPathway.path.Count()-1);
                            }
                    }
                        break;
                    case 1:
                        nextLocation = moveTo_SolveAllPaths(grid[currentLocation[0], currentLocation[1]], firstPathway.availableMoves.ElementAt(0));
                        break;
                    case 2:
                        nextLocation = moveTo_SolveAllPaths(grid[currentLocation[0], currentLocation[1]], firstPathway.availableMoves.ElementAt(0));
                        break;
                    case 3:
                        nextLocation = moveTo_SolveAllPaths(grid[currentLocation[0], currentLocation[1]], firstPathway.availableMoves.ElementAt(0));
                        break;
                    case 4:
                        nextLocation = moveTo_SolveAllPaths(grid[currentLocation[0], currentLocation[1]], firstPathway.availableMoves.ElementAt(0));
                        break;
                    default:
                        break;
                }

                //nextLocation = moveTo_SolveAllPaths(grid[currentLocation[0], currentLocation[1]], firstPathway.availableMoves.ElementAt(0));
                firstPathway.availableMoves.Clear();
                firstPathway.path.Add(nextLocation);
                Console.WriteLine($"\n current: ({currentLocation[0]},{currentLocation[1]}) next: ({nextLocation[0]}, {nextLocation[1]})\n");
            }

            foreach (var item in firstPathway.path) 
            {
                grid[item[0], item[1]].partOfFirstPath = true;
            }
            updateBitmap();
            return firstPathway;
        }
        private List<Pathway.movement> getPossibleMoves_VisitedNotPossible(int[] currentLocation)
        {
            List<Pathway.movement> availableMoves = new List<Pathway.movement>();

            if (!grid[currentLocation[0], currentLocation[1]].topWall && !grid[currentLocation[0], currentLocation[1] - 1].partOfPath) // if this fails, move to nested if loop
            {
                availableMoves.Add(Pathway.movement.up);
            }

            if (!grid[currentLocation[0], currentLocation[1]].leftWall && !grid[currentLocation[0] - 1, currentLocation[1]].partOfPath) // if this fails, move to nested if loop
            {
                availableMoves.Add(Pathway.movement.left);
            }

            if (!grid[currentLocation[0], currentLocation[1]].rightWall && !grid[currentLocation[0] + 1, currentLocation[1]].partOfPath) // if this fails, move to nested if loop
            {
                availableMoves.Add(Pathway.movement.right);
            }

            if (!grid[currentLocation[0], currentLocation[1]].bottomWall && !grid[currentLocation[0], currentLocation[1] + 1].partOfPath) // if this fails, move to nested if loop
            {
                availableMoves.Add(Pathway.movement.down);
            }

            return availableMoves;            
        }
        private int[] moveTo_SolveAllPaths(cell currentCell, Pathway.movement next)
        {
            int[] newLocation = new int[3];

            newLocation[0] = currentCell.i;
            newLocation[1] = currentCell.j;
            newLocation[2] = 0;

            switch (next)
            {
                case Pathway.movement.empty:
                    break;
                case Pathway.movement.left:
                    newLocation[0]--;
                    break;
                case Pathway.movement.right:
                    newLocation[0]++;
                    break;
                case Pathway.movement.up:
                    newLocation[1]--;
                    break;
                case Pathway.movement.down:
                    newLocation[1]++;
                    break;
                case Pathway.movement.stay:
                    break;
                default:
                    break;
            }

            return newLocation;
        }
        #endregion


        #region Genetic Algorithm
        private void solveGeneticAlgorithm()
        {
            if (!mazeComplete)
            {
                instantSolve();
            }


            int n = 10;
            int totalMoves = 150;

            Population[] p = new Population[n];

            // initialize population
            for (int i = 0; i < n; i++)
            {
                p[i] = new Population();
                p[i].init(totalMoves);
            }

            // calculate fitness for population
            for (int i = 0; i < n; i++)
            {
                p[i].fitness = calcFitness_AllMovesPossible(p[i], totalMoves);
            }

        }
        public double calcFitness_AllMovesPossible(Population p, int totalMoves)
        {
            double fitness = 0;
            int wallsHit = 0;
            int distToEnd = rows + cols;

            // calc walls hit
            //for (int i = 0; i < totalMoves; i++)
            //{
            //    if (hitwall)
            //    {
            //        wallsHit++;
            //    }
            //}

            // calc final distance to end
            int i;
            int j;

            i = p.path.Last()[0];
            j = p.path.Last()[1];
            distToEnd = (cols - i) + rows - j;
                        
            return fitness;
        }
        #endregion
    }

    public class cell
    {
        public int i;
        public int j;

        public bool visited = false;
        public bool current = false;

        public int formWidth;
        public int formHeight;
        int width;
        int height;

        public bool topWall = true;
        public bool leftWall = true;
        public bool bottomWall = true;
        public bool rightWall = true;

        public Rectangle rectLoc = new Rectangle();

        public int offset;

        public List<cell> neighborList = new List<cell>();

        public List<lines> linesCollection = new List<lines>();

        public bool partOfPath = false;
        public bool partOfFirstPath = false;


        public enum pos
        {
            empty,
            above,
            left,
            below,
            right,            
        }

        public enum wallType
        {
            topWall,
            bottomWall,
            leftWall,
            rightWall
        }

        public void create()
        {
            lines topLine = new lines();
            lines bottomLine = new lines();
            lines leftLine = new lines();
            lines rightLine = new lines();

            offset = Form1.offset;


            width = (Form1.borderList.ElementAt(0).x2 - Form1.borderList.ElementAt(0).x1) / Form1.cols;
            height = (Form1.borderList.ElementAt(1).y2 - Form1.borderList.ElementAt(1).y1) / Form1.rows;

            if (width * Form1.cols > height * Form1.rows)
            {
                width = height;
            }
            else
            {
                height = width;
            }

            int x1, x2, y1, y2;

            x1 = (i * width);
            y1 = (j * height);
            x2 = (i * width) + width;
            y2 = (j * height) + height;

            rectLoc.X = (x1);// + 1);
            rectLoc.Y = (y1);// + 1);
            rectLoc.Width = width;// - 1;
            rectLoc.Height = width;// - 1;

            topLine.wallType = wallType.topWall;
            bottomLine.wallType = wallType.bottomWall;
            leftLine.wallType = wallType.leftWall;
            rightLine.wallType = wallType.rightWall;

            topLine.x1 = x1;
            topLine.y1 = y1;
            topLine.x2 = x2;
            topLine.y2 = y1;

            leftLine.x1 = x1;
            leftLine.y1 = y1;
            leftLine.x2 = x1;
            leftLine.y2 = y2;

            rightLine.x1 = x2;
            rightLine.y1 = y1;
            rightLine.x2 = x2;
            rightLine.y2 = y2;

            bottomLine.x1 = x1;
            bottomLine.y1 = y2;
            bottomLine.x2 = x2;
            bottomLine.y2 = y2;

            linesCollection.Add(topLine);
            linesCollection.Add(leftLine);
            linesCollection.Add(bottomLine);
            linesCollection.Add(rightLine);
        }
    }
    public class lines
    {
        public cell.wallType wallType;        

        public int x1;
        public int y1;
        public int x2;
        public int y2;
    }


    public class Pathway
    {
        public List<int[]> path = new List<int[]>();
        public bool finished = false;
        public movement lastPathChoice = movement.empty; // 0 = empty, 1 = left, 2 = right, 3 = up, 4 = down, 5 = stay
        public List<movement> availableMoves = new List<movement>();
        public int movementChoice = 0;
        public enum movement
        {
            empty,
            left,
            right,
            up,
            down,
            stay
        }
    }

    public class Population
    {
        public List<int[]> path = new List<int[]>();
        int[] currentCell = new int[2];

        public double fitness = 0;

        Random r = new Random(DateTime.Now.Millisecond);

        enum next
        {
            empty,
            left,
            right,
            up,
            down,
            stay
        }

        // add cell to path
        // move to next cell
         
        public void init(int size)
        {
            currentCell[0] = Form1.startX;
            currentCell[0] = Form1.startY;


            for (int i = 0; i < size; i++)
            { 
                path.Add(currentCell);

                currentCell = moveToNextCell_AllMovesPossible(currentCell);

                if (currentCell[0] == Form1.endX && currentCell[i] == Form1.endY)
                {
                    //stay here
                }
            }
        }
        
        private int[] moveToNextCell_AllMovesPossible(int[] currentCell)
        {
            int i = currentCell[0];
            int j = currentCell[1];
            bool possibleMove = false;
            next nextCell = new next();

            while (!possibleMove)
            {
                nextCell = (next)r.Next(1, 5);

                switch (nextCell)
                {
                    case next.empty:
                        break;
                    case next.left:
                        i--;
                        break;
                    case next.right:
                        i++;
                        break;
                    case next.up:
                        j--;
                        break;
                    case next.down:
                        j++;
                        break;
                    default:
                        break;
                }

                if (i >= 0 && i <= Form1.cols-1 && j >= 0 && j <= Form1.rows - 1)
                {
                    possibleMove = true;
                }
                else
                {
                    i = currentCell[0];
                    j = currentCell[1];
                }
            }
            int[] newCell = new int[2];
            newCell[0] = i;
            newCell[1] = j;

            Console.WriteLine(nextCell.ToString());
            Console.WriteLine($"({i}, {j})\n");

            return newCell;
        }

        

    }
}
