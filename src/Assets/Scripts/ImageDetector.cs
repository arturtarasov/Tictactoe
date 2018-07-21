using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UnityEngine;

namespace Assets.Scripts
{
    public class ImageDetector : MonoBehaviour
    {
        [SerializeField] private SeineParams seineParams;
        [SerializeField] private SeineParams seineParamsForButton;

        [SerializeField] private ShowState showState;
        [SerializeField]
        private DetectHsv hsvState;
        private DetectHsv hsvStateLast;

        [SerializeField] private bool drawCalibrationButtons;

        [Header("For OpenCV marker detection")] [SerializeField] private HSV hsvMarker;
        [Header("For OpenCV color detection")] [SerializeField] private HSV hsvCoin;
        [Header("For OpenCV laser detection")] [SerializeField] private HSV hsvLaser;

        private Image<Bgr, Byte> currentFrame; //оригинальное изображение с камеры

        /// <summary>
        /// Картинка с emgucv (оригинальная)
        /// </summary>
        private Capture capture;


        private Image<Gray, byte>[] channels; //черно-белое изображение с лазером

        /// <summary>
        /// Текстура для преобразования в unity - формат
        /// </summary>
        private Texture2D cameraTexture;
        
        [Header("Line Settings")] [SerializeField] private double rhoResolution = 1;
        [SerializeField] private double thetaResolution = Math.PI/45.0;
        [SerializeField] private int threshold = 5;
        [SerializeField] private double minLineWidth = 30;
        [SerializeField] private double gapBetweenLines = 10;
        
        //центр масс для калибрования лазером
        [SerializeField] private int screenX; 
        [SerializeField] private int screenY;

        /// <summary>
        /// массив индексов в ячейках белых точек
        /// </summary>
        private List<Vector2> points;

        public ScreenToWorld buttonRectLU = new ScreenToWorld(new Rect(50, 50, 30, 30));
        private bool buttonPressedLU;
        
        public ScreenToWorld buttonRectLD = new ScreenToWorld(new Rect(50, 100, 30, 30));
        private bool buttonPressedLD;
        
        public ScreenToWorld buttonRectRU = new ScreenToWorld(new Rect(100, 50, 30, 30));
        private bool buttonPressedRU;
        
        public ScreenToWorld buttonRectRD = new ScreenToWorld(new Rect(100, 100, 30, 30));
        private bool buttonPressedRD;

        //последние позиции точек LU,LD,RU,RD
        private Rect buttonRectLU_last;
        private Rect buttonRectRU_last;
        private Rect buttonRectLD_last;
        private Rect buttonRectRD_last;
        
        /// <summary>
        /// Здесь хранятся результаты ЧЕЛОВЕКА
        /// </summary>
        private List<Vector2> elementsMarker = new List<Vector2>();
        /// <summary>
        /// Здесь хранятся результаты БОТА
        /// </summary>
        private List<Vector2> elementsPlayer = new List<Vector2>();
        
        private int iterator;
        private int[,] board = new int[3, 3];
        private int currentState;
        
        /// <summary>
        /// счётчики белых точек по каждой из ячеек
        /// </summary>
        public int i00 = 0, i01 = 0, i02 = 0, i10 = 0, i11 = 0, i12 = 0, i20 = 0, i21 = 0, i22 = 0;

        /// <summary>
        /// время в секундах на калибровку каждой точки
        /// </summary>
        [SerializeField]
        private float calibrationsTimer = 3.0f;

        /// <summary>
        /// минимальное количество белых точек для определения
        /// </summary>
        [SerializeField]
        private int minimumToDetect = 100;

        /// <summary>
        /// строится сетка
        /// </summary>
        /// <param name="seineParams"></param>
        /// <returns>возвращается построенная сетка (массивы прямых)</returns>
        private SeineParams BuildChain(SeineParams seineParams) //строится сетка
        {
            seineParams.seine = new Vector2[4, seineParams.resolution + 1]; //здесь заполняю массив из точек, которые были получены делением прямых             //хранение всех точек при пересечениях прямых?

            //по точкам строю прямые LU-LD, LU-RU, RU-RD, RD-LD, затем бью их на 3 по длине, и ищу точки разбиения                                              //прямые строится не через GUIHelper?
            for (int i = 0; i < seineParams.resolution + 1; i++)
            {
                seineParams.seine[0, i] =
                    new Vector2(
                        buttonRectLU.World.x +
                        (buttonRectRU.World.x - buttonRectLU.World.x)/(1.0f*seineParams.resolution)*i,
                        buttonRectLU.World.y +
                        (buttonRectRU.World.y - buttonRectLU.World.y)/(1.0f*seineParams.resolution)*i);
                seineParams.seine[1, i] =
                    new Vector2(
                        buttonRectRU.World.x +
                        (buttonRectRD.World.x - buttonRectRU.World.x)/(1.0f*seineParams.resolution)*i,
                        buttonRectRU.World.y +
                        (buttonRectRD.World.y - buttonRectRU.World.y)/(1.0f*seineParams.resolution)*i);
                seineParams.seine[2, i] =
                    new Vector2(
                        buttonRectLD.World.x +
                        (buttonRectRD.World.x - buttonRectLD.World.x)/(1.0f*seineParams.resolution)*i,
                        buttonRectLD.World.y +
                        (buttonRectRD.World.y - buttonRectLD.World.y)/(1.0f*seineParams.resolution)*i);
                seineParams.seine[3, i] =
                    new Vector2(
                        buttonRectLU.World.x +
                        (buttonRectLD.World.x - buttonRectLU.World.x)/(1.0f*seineParams.resolution)*i,
                        buttonRectLU.World.y +
                        (buttonRectLD.World.y - buttonRectLU.World.y)/(1.0f*seineParams.resolution)*i);
            }
            //здесь заполняются коэффициенты прямых k и b - ищу по пересечениям прямых, полученных выше
            seineParams.koef = new Vector2[2, seineParams.resolution + 1];

            float k, b;

            for (int i = 0; i < seineParams.resolution + 1; i++)
            {
                k = (seineParams.seine[2, i].y - seineParams.seine[0, i].y)/
                    (seineParams.seine[2, i].x - seineParams.seine[0, i].x);
                b = seineParams.seine[0, i].y - k*seineParams.seine[0, i].x;
                seineParams.koef[0, i] = new Vector2(k, b);
            }

            for (int i = 0; i < seineParams.resolution + 1; i++)
            {
                k = (seineParams.seine[3, i].y - seineParams.seine[1, i].y)/
                    (seineParams.seine[3, i].x - seineParams.seine[1, i].x);
                b = seineParams.seine[1, i].y - k*seineParams.seine[1, i].x;
                seineParams.koef[1, i] = new Vector2(k, b);
            }
            return seineParams;
        }
        /// <summary>
        /// Unity default
        /// </summary>
        private void Start()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    board[i, j] = -1; //очистка поле при начале игры
                }
            }

            capture = new Capture(); //захват изображения (emgucv default)

            capture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, 640); //настройка картиники, чтобы бралась картинка 640х480
            capture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, 480); //настройка картиники, чтобы бралась картинка 640х480
        }
        /// <summary>
        /// проверка каждого белого пикселя изображения на наличие в сетке - и возврат id ячейки, в которую входит белый пиксель
        /// </summary>
        /// <param name="laserPoint"></param>
        /// <returns>координаты сетки</returns>
        private Vector2 FindNearPoint(Vector2 laserPoint)
        {
            int returnX = -1, returnY = -1;

            for (int i = 0; i < seineParams.resolution && returnX == -1; i++)                                                                           
            {
                if (
                    (laserPoint.x > (laserPoint.y - seineParams.koef[0, i].y)/seineParams.koef[0, i].x) &&                                                              
                    (laserPoint.x < (laserPoint.y - seineParams.koef[0, i + 1].y)/seineParams.koef[0, i + 1].x)
                    )
                //проверка уравнения прямой. x > (y1-b)/k1, x < (y2-b)/k2
                {
                    returnX = i;
                }
            }

            for (int i = 0; i < seineParams.resolution && returnY == -1; i++)
            {
                if (
                    (laserPoint.y > laserPoint.x*seineParams.koef[1, i].x + seineParams.koef[1, i].y) &&
                    (laserPoint.y < laserPoint.x*seineParams.koef[1, i + 1].x + seineParams.koef[1, i + 1].y)
                    )
                //проверка уравнения прямой. y > k1 * x + b1, y < k2 * x + b2
                {
                    returnY = i;
                }
            }
            return new Vector2(returnX, returnY);
        }
        /// <summary>
        /// если одна точка сетки поменяла позицию - перестраиваем
        /// </summary>
        private void CheckChainUpdate()
        {
            if (buttonRectLU_last != buttonRectLU.Rectangle) //если хотя бы одна точка сетки (LU,RU,LD,RD) сдвинулась - перестраиваю сетку
            {
                seineParams = BuildChain(seineParams);
                seineParamsForButton = BuildChain(seineParamsForButton);
                buttonRectLU_last = buttonRectLU.Rectangle;
            }

            if (buttonRectRU_last != buttonRectRU.Rectangle) //если хотя бы одна точка сетки (LU,RU,LD,RD) сдвинулась - перестраиваю сетку
            {
                seineParams = BuildChain(seineParams);
                seineParamsForButton = BuildChain(seineParamsForButton);
                buttonRectRU_last = buttonRectRU.Rectangle;
            }

            if (buttonRectLD_last != buttonRectLD.Rectangle) //если хотя бы одна точка сетки (LU,RU,LD,RD) сдвинулась - перестраиваю сетку
            {
                seineParams = BuildChain(seineParams);
                seineParamsForButton = BuildChain(seineParamsForButton);
                buttonRectLD_last = buttonRectLD.Rectangle;
            }

            if (buttonRectRD_last != buttonRectRD.Rectangle) //если хотя бы одна точка сетки (LU,RU,LD,RD) сдвинулась - перестраиваю сетку
            {
                seineParams = BuildChain(seineParams);
                seineParamsForButton = BuildChain(seineParamsForButton);
                buttonRectRD_last = buttonRectRD.Rectangle;
            }
        }
        /// <summary>
        /// Unity default
        /// </summary>
        private void Update()
        {
            CheckChainUpdate();

            if (capture != null)
            {
                currentFrame = capture.RetrieveBgrFrame();
                
                using (Image<Hsv, byte> hsvImagev = currentFrame.Convert<Hsv, byte>())  //стандартная функция emgu cv, конвертируем в hsv формат картинки
                {
                    channels = hsvImagev.Split();  //стандартная функция emgu cv
                    //0 - канал оттенков, 1 - канал насыщенности, 2 - канал значений

                    switch (hsvState)
                    {
                        case DetectHsv.CoinHsv:
                            channels[0] = hsvImagev.InRange(new Hsv(hsvCoin.minH, hsvCoin.minS, hsvCoin.minV),                              //возвращает черно - белую картинку с интервалом(min, max)
                                new Hsv(hsvCoin.maxH, hsvCoin.maxS, hsvCoin.maxV));
                            break;
                        case DetectHsv.LaserHsv:
                            channels[0] = hsvImagev.InRange(new Hsv(hsvLaser.minH, hsvLaser.minS, hsvLaser.minV),
                                new Hsv(hsvLaser.maxH, hsvLaser.maxS, hsvLaser.maxV));
                            break;
                        case DetectHsv.MarkerHsv:
                            channels[0] = hsvImagev.InRange(new Hsv(hsvMarker.minH, hsvMarker.minS, hsvMarker.minV),
                                new Hsv(hsvMarker.maxH, hsvMarker.maxS, hsvMarker.maxV));
                            break;
                    }

                    if (points == null)
                    {
                        points = new List<Vector2>();
                    }
                    if (points != null)
                    {
                        points.Clear();
                    }
                    int summX = 0, summY = 0;
                    int range = 0;
                    
                    //channels[0].Data.GetLength(0) = 480 channels[0].Data.GetLength(1) = 640
                    for (int i = 0; i < channels[0].Data.GetLength(0); i++)
                    {
                        for (int k = 0; k < channels[0].Data.GetLength(1); k++)
                        {
                            if ((int) channels[0].Data[i, k, 0] > 0)       //стандартная функция emgu cv по "вытаскиванию" пикселей из матрицы ширина*высота, проверка: 0 - черный, 1 - белый
                            {
                                points.Add(FindNearPoint(new Vector2(k, i))); //добавляем все индексы сетки, в которые входит пиксель. Если не входит: хотя бы одно значение будет =-1

                                summX += i;
                                summY += k;
                                range++;
                            }
                        }
                    }
                    if (range != 0) //ищу центр масс для калибровки лазером
					{
						screenY = summX/range;
						screenX = summY/range;
					}
                    if (cameraTexture == null)
                    {
                        cameraTexture = new Texture2D(640, 480);
                    }
                    if (cameraTexture != null)
                    {
                        if (showState == ShowState.Original)
                        {
                            cameraTexture.LoadImage(currentFrame.ToJpegData()); //стандартная функция emgu cv ToJpegData() - преобразовывает картинки из emgu cv в удобный формат для Unity
                        }
                        if (showState == ShowState.Gray)
                        {
                            cameraTexture.LoadImage(channels[0].ToJpegData()); //стандартная функция emgu cv ToJpegData() - преобразовывает картинки из emgu cv в удобный формат для Unity
                        }
                    }
                    GetStats();
                }
            }
        }
        /// <summary>
        /// все пиксели, посчитанные в сетке - считаем количество в каждом элементе сетки
        /// </summary>
        private void GetStats()
        {
            float TOLERANCE = 0.1f;

            i00 = 0;
            i01 = 0;
            i02 = 0;
            i10 = 0;
            i11 = 0;
            i12 = 0;
            i20 = 0;
            i21 = 0;
            i22 = 0;
            // value < TOLERANCE - необходимо потому, что нельзя просто так сравнить два float значения
            foreach (var point in points)                                                                                            
            {
                if (Math.Abs(point.x) < TOLERANCE) // if(point.x == 0.0f)
                {
                    if (Math.Abs(point.y) < TOLERANCE) // if(point.y == 0.0f)
                    {
                        i00++;
                    }
                    if (Math.Abs(point.y - 1.0f) < TOLERANCE)  // if(point.y == 1.0f)
                    {
                        i01++;
                    }
                    if (Math.Abs(point.y - 2.0f) < TOLERANCE)  // if(point.y == 2.0f)
                    {
                        i02++;
                    }
                }
                if (Math.Abs(point.x - 1.0f) < TOLERANCE) // if(point.x == 1.0f)
                {
                    if (Math.Abs(point.y) < TOLERANCE) // if(point.y == 0.0f)
                    {
                        i10++;
                    }
                    if (Math.Abs(point.y - 1.0f) < TOLERANCE) // if(point.y == 0.0f)
                    {
                        i11++;
                    }
                    if (Math.Abs(point.y - 2.0f) < TOLERANCE) // if(point.y == 0.0f)
                    {
                        i12++;
                    }
                }
                if (Math.Abs(point.x - 2.0f) < TOLERANCE) // if(point.x == 2.0f)
                {
                    if (Math.Abs(point.y) < TOLERANCE) // if(point.y == 0.0f)
                    {
                        i20++;
                    }
                    if (Math.Abs(point.y - 1.0f) < TOLERANCE) // if(point.y == 0.0f)
                    {
                        i21++;
                    }
                    if (Math.Abs(point.y - 2.0f) < TOLERANCE) // if(point.y == 0.0f)
                    {
                        i22++;
                    }
                }
            }
            //минимум должно в ячейке быть такое количество белых пикселей
            if (i00 > minimumToDetect) AddElement(0, 0);
            if (i01 > minimumToDetect) AddElement(0, 1);
            if (i02 > minimumToDetect) AddElement(0, 1);
            if (i10 > minimumToDetect) AddElement(1, 0);
            if (i11 > minimumToDetect) AddElement(1, 1);
            if (i12 > minimumToDetect) AddElement(1, 2);
            if (i20 > minimumToDetect) AddElement(2, 0);
            if (i21 > minimumToDetect) AddElement(2, 1);
            if (i22 > minimumToDetect) AddElement(2, 2);
        }
        /// <summary>
        /// добавить элемент сетки (х)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void AddElement(int x, int y)
        {
            if (elementsMarker.IndexOf(new Vector2(x, y)) == -1)
            {
                elementsMarker.Add(new Vector2(x, y)); //ходит ЧЕЛОВЕК
                board[x, y] = 1;
            }
        }
        /// <summary>
        /// Проверка игры на окончание
        /// </summary>
        /// <returns>-1 игра не закончена, 0 выиграли нолики, 1 выиграли крестики, 2 ничья</returns>
        private int IsGameEnded()
        {
            int BoardSize = 3;
            for (int i = 0; i < BoardSize; i++) //три в ряд по Х
            {
                iterator = 0;
                currentState = board[i, 0];
                for (int j = 1; j < BoardSize; j++)
                {
                    if (currentState != board[i, j])
                    {
                    }
                    else
                    {
                        iterator++;
                    }
                }
                if (iterator == BoardSize - 1)
                {
                    return currentState;
                }
            }

            for (int j = 0; j < BoardSize; j++) //три в ряд по У
            {
                iterator = 0;
                currentState = board[0, j];
                for (int i = 1; i < BoardSize; i++)
                {
                    if (currentState != board[i, j])
                    {
                    }
                    else
                    {
                        iterator++;
                    }
                }
                if (iterator == BoardSize - 1)
                {
                    return currentState;
                }
            }

            iterator = 0;
            currentState = board[0, 0];
            for (int i = 1; i < BoardSize; i++) //одна диагональ
            {
                if (currentState != board[i, i])
                {
                }
                else
                {
                    iterator++;
                }
            }
            if (iterator == BoardSize - 1)
            {
                return currentState;
            }

            iterator = 0;
            currentState = board[0, BoardSize - 1];
            for (int i = 1; i < BoardSize; i++) //другая диагональ
            {
                if (currentState != board[i, BoardSize - 1 - i])
                {
                }
                else
                {
                    iterator++;
                }
            }
            if (iterator == BoardSize - 1)
            {
                return currentState;
            }
            for (int i = 0; i < BoardSize; i++) //  игра не завершилась
            {
                for (int j = 0; j < BoardSize; j++)
                {
                    if (board[i, j] == -1)
                    {
                        return -1;
                    }
                }
            }
            return 2; //ничья
        }
        /// <summary>
        /// если количество х больше количества о - строим о
        /// </summary>
        public void CreateMove()
        {
            if (elementsPlayer.Count < elementsMarker.Count) //если количество х больше количества о - строим о
            {
                List<VectorInt> emptyCellsList = new List<VectorInt>();
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        switch (board[i, j])
                        {
                            case -1:
                                emptyCellsList.Add(new VectorInt() {X = i, Y = j});
                                break;
                        }
                    }
                }
                if (emptyCellsList.Count > 0)
                {
                    System.Random r = new System.Random();
                    int randomIndex = r.Next(0, emptyCellsList.Count);

                    elementsPlayer.Add(new Vector2(emptyCellsList[randomIndex].X, emptyCellsList[randomIndex].Y)); //ходит БОТ
                    board[emptyCellsList[randomIndex].X, emptyCellsList[randomIndex].Y] = 0;
                }
            }
        }
        /// <summary>
        /// Unity default
        /// </summary>
        private void OnGUI()
        {
            if (cameraTexture != null)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), cameraTexture);
            }

            if (seineParams.DrawSeineGUI) //отрисовать сетку 3х3
            {
                for (int i = 0; i < seineParams.resolution + 1; i++)
                {
                    GuiHelper.DrawLine(new Vector2(seineParams.seine[0, i].x, seineParams.seine[0, i].y),
                        new Vector2(seineParams.seine[2, i].x, seineParams.seine[2, i].y), UnityEngine.Color.red);
                    GuiHelper.DrawLine(new Vector2(seineParams.seine[1, i].x, seineParams.seine[1, i].y),
                        new Vector2(seineParams.seine[3, i].x, seineParams.seine[3, i].y), UnityEngine.Color.blue);
                }
            }

            if (seineParamsForButton.DrawSeineGUI) //отрисовать сетку 6х6
            {
                for (int i = 1; i < seineParamsForButton.resolution + 1; i = i + 2)
                {
                    GuiHelper.DrawLine(
                        new Vector2(seineParamsForButton.seine[0, i].x, seineParamsForButton.seine[0, i].y),
                        new Vector2(seineParamsForButton.seine[2, i].x, seineParamsForButton.seine[2, i].y),
                        UnityEngine.Color.green);
                    GuiHelper.DrawLine(
                        new Vector2(seineParamsForButton.seine[1, i].x, seineParamsForButton.seine[1, i].y),
                        new Vector2(seineParamsForButton.seine[3, i].x, seineParamsForButton.seine[3, i].y),
                        UnityEngine.Color.white);
                }
            }
#region Отрисовка калибровочных кнопок
            if (drawCalibrationButtons)
            {
                if (buttonRectLU.Rectangle.Contains(Event.current.mousePosition))
                {
                    switch (Event.current.type)
                    {
                        case EventType.MouseDown:
                            buttonPressedLU = true;
                            break;
                        case EventType.MouseUp:
                            buttonPressedLU = false;
                            break;
                    }
                }
                if (buttonPressedLU && Event.current.type == EventType.MouseDrag)
                {
                    buttonRectLU.Rectangle = new Rect(buttonRectLU.Rectangle.x + Event.current.delta.x,
                        buttonRectLU.Rectangle.y + Event.current.delta.y,
                        30, 30);
                }
                GUI.Button(buttonRectLU.Rectangle, "LU");

                if (buttonRectLD.Rectangle.Contains(Event.current.mousePosition)) //если мышка внутри кнопки LU и кнопка зажата - двигаем
                {
                    switch (Event.current.type)
                    {
                        case EventType.MouseDown:
                            buttonPressedLD = true;
                            break;
                        case EventType.MouseUp:
                            buttonPressedLD = false;
                            break;
                    }
                }
                if (buttonPressedLD && Event.current.type == EventType.MouseDrag)
                {
                    buttonRectLD.Rectangle = new Rect(buttonRectLD.Rectangle.x + Event.current.delta.x,
                        buttonRectLD.Rectangle.y + Event.current.delta.y,
                        30, 30);
                }
                GUI.Button(buttonRectLD.Rectangle, "LD");

                if (buttonRectRU.Rectangle.Contains(Event.current.mousePosition))
                {
                    switch (Event.current.type)
                    {
                        case EventType.MouseDown:
                            buttonPressedRU = true;
                            break;
                        case EventType.MouseUp:
                            buttonPressedRU = false;
                            break;
                    }
                }
                if (buttonPressedRU && Event.current.type == EventType.MouseDrag)
                {
                    buttonRectRU.Rectangle = new Rect(buttonRectRU.Rectangle.x + Event.current.delta.x,
                        buttonRectRU.Rectangle.y + Event.current.delta.y,
                        30, 30);
                }
                GUI.Button(buttonRectRU.Rectangle, "RU");

                if (buttonRectRD.Rectangle.Contains(Event.current.mousePosition))
                {
                    switch (Event.current.type)
                    {
                        case EventType.MouseDown:
                            buttonPressedRD = true;
                            break;
                        case EventType.MouseUp:
                            buttonPressedRD = false;
                            break;
                    }
                }
                if (buttonPressedRD && Event.current.type == EventType.MouseDrag)
                {
                    buttonRectRD.Rectangle = new Rect(buttonRectRD.Rectangle.x + Event.current.delta.x,
                        buttonRectRD.Rectangle.y + Event.current.delta.y,
                        30, 30);
                }
                GUI.Button(buttonRectRD.Rectangle, "RD");

                if (GUI.Button(new Rect(25, 25, 150, 20), "Калибровать лазером"))
                {
                    StartCoroutine("CalibrateLU");
                }
                if (GUI.Button(new Rect(25, 50, 150, 20), "Очистить поле"))
                {
                    for (int i = 0; i < board.GetLength(0); i++)
                    {
                        for (int j = 0; j < board.GetLength(1); j++)
                        {
                            board[i, j] = -1;
                        }
                    }
                    elementsMarker.Clear();
                    elementsPlayer.Clear();
                }
            }
#endregion

            if (hsvState == DetectHsv.LaserHsv)
            {
                return;
            }

            for (int i = 1; i < seineParamsForButton.resolution + 1; i = i + 2)
            {
                for (int j = 1; j < seineParamsForButton.resolution + 1; j = j + 2)
                {
                    float x = -1*(seineParamsForButton.koef[0, i].y - seineParamsForButton.koef[1, j].y)/
                              (seineParamsForButton.koef[0, i].x - seineParamsForButton.koef[1, j].x);
                    float y = seineParamsForButton.koef[0, i].x*x + seineParamsForButton.koef[0, i].y;


                    string content = "";
                    float TOLERANCE = 0.1f;

                    foreach (var element in elementsMarker) //перебираем все элементы БОТА
                    {
                        if (Math.Abs(element.x - (i - 1)/2.0f) < TOLERANCE &&   // |element.x == (i-1)/2|
                            Math.Abs(element.y - (j - 1)/2.0f) < TOLERANCE)     // |element.y == (j-1)/2|
                        {
                            content = "X";
                        }
                    }

                    foreach (var element in elementsPlayer)
                    {
                        if (Math.Abs(element.x - (i - 1)/2.0f) < TOLERANCE &&  // |element.x == (i-1)/2|
                            Math.Abs(element.y - (j - 1)/2.0f) < TOLERANCE)    // |element.y == (j-1)/2|
                        {
                            content = "O";
                        }
                    }
                    CreateMove();
                    if (GUI.Button(new Rect(x - 10, y - 10, 20, 20), content))
                    {
                        if (content == "")
                        {
                        }
                    }
                }
            }
            if (IsGameEnded() == 1)
            {
                Debug.Log("Победили крестики");
            }
            if (IsGameEnded() == 0)
            {
                Debug.Log("Нолики победили");
            }
            if (IsGameEnded() == 2)
            {
                Debug.Log("Ничья");
            }
        }
        /// <summary>
        /// Калибровка точки
        /// </summary>
        /// <returns></returns>
        private IEnumerator CalibrateLU()
        {
            hsvStateLast = hsvState;
            hsvState = DetectHsv.LaserHsv;

            Debug.Log("Откалибруйте левую верхнюю точку");
            yield return new WaitForSeconds(calibrationsTimer);
            buttonRectLU.Rectangle = new Rect(screenX, screenY, 30, 30);
            StartCoroutine("CalibrateRU");
        }
        /// <summary>
        /// Калибровка точки
        /// </summary>
        /// <returns></returns>
        private IEnumerator CalibrateRU()
        {
            Debug.Log("Откалибруйте правую верхнюю точку");
            yield return new WaitForSeconds(calibrationsTimer);
            buttonRectRU.Rectangle = new Rect(screenX, screenY, 30, 30);
            StartCoroutine("CalibrateLD");
        }
        /// <summary>
        /// Калибровка точки
        /// </summary>
        /// <returns></returns>
        private IEnumerator CalibrateLD()
        {
            Debug.Log("Откалибруйте левую нижнюю точку");
            yield return new WaitForSeconds(calibrationsTimer);
            buttonRectLD.Rectangle = new Rect(screenX, screenY, 30, 30);
            StartCoroutine("CalibrateRD");
        }
        /// <summary>
        /// Калибровка точки
        /// </summary>
        /// <returns></returns>
        private IEnumerator CalibrateRD()
        {
            Debug.Log("Откалибруйте правую нижнюю точку");
            yield return new WaitForSeconds(calibrationsTimer);
            buttonRectRD.Rectangle = new Rect(screenX, screenY, 30, 30);

            hsvState = hsvStateLast;
        }
    }
}