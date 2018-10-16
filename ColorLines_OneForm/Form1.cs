using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using LibForColorLines;

namespace ColorLines_OneForm
{
    public partial class Form1 : Form
    {
        Engine ex = new Engine();

        public Form1()
        {
            InitializeComponent();
            cbxSizeOfField.SelectedIndex = 1;
        }

        private void btnRules_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Для начала выберите размер поля и нажмите Новая игра. \nЦель игры - набрать как можно больше очков. Составляйте линии из шаров одинакового цвета. Линии из пяти шаров убираются с поля. Вы можете переставить шар, если путь к указанной вами точке не перекрыт другими шарами. \nУдачи :)", "Правила");
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show("ColorLines v1.4 by Spmart, june 2015", "Об игре");
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private int[,] field; //Инициализируем игровое поле

        private void btnNewGame_Click(object sender, EventArgs e)
        {
            btnSaveGame.Enabled = true;
            pictureBox1.Visible = true; //Показываем наш pictureBox
            ex.SetSizeOfField(Convert.ToInt32(cbxSizeOfField.SelectedItem));
            ex.SetScore(0); 
            field = new int [ex.GetSizeOfField(), ex.GetSizeOfField()];
            ex.SetupBalls(field);
            DrawField(field);
            pictureBox1.Enabled = true;
            this.Size = new Size(pictureBox1.Width + 300, pictureBox1.Height + 100);
        }

        private void btnSaveGame_Click(object sender, EventArgs e)
        {
            using (StreamWriter stream = new StreamWriter("save.sav"))
            {
                stream.WriteLine(ex.GetSizeOfField());
                stream.WriteLine(ex.GetScore());
                for (int i = 0; i < ex.GetSizeOfField(); i++)
                    for (int j = 0; j < ex.GetSizeOfField(); j++)
                        stream.Write(field[i, j]);
            }
            MessageBox.Show("Игра сохранена!", "Сохранение");
        }

        private void btnLoadGame_Click(object sender, EventArgs e)
        {
            try
            {
                using (StreamReader stream = new StreamReader("save.sav"))
                {
                    ex.SetSizeOfField(Convert.ToInt32(stream.ReadLine()));
                    ex.SetScore(Convert.ToInt32(stream.ReadLine()));
                    field = new int[ex.GetSizeOfField(), ex.GetSizeOfField()];
                    for (int i = 0; i < ex.GetSizeOfField(); i++)
                        for (int j = 0; j < ex.GetSizeOfField(); j++)
                            field[i, j] = Convert.ToInt32(stream.Read()) - 48; //Т.к. Read читает символы
                }
                pictureBox1.Visible = true;
                pictureBox1.Enabled = true;
                btnSaveGame.Enabled = true;
                cbxSizeOfField.Text = ex.GetSizeOfField().ToString();
                DrawField(field);
                this.Size = new Size(pictureBox1.Width + 300, pictureBox1.Height + 100);
                MessageBox.Show("Игра загружена!", "Загрузка");
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("Файл сохранения не найден!", "Ошибка!");
            }
        }

        private bool ballIsChoosen = false; //Флаг выбран шар или нет. При первом ходе не выбран
        private int fromX, fromY; //Откуда будем переставлять шар

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            //DrawField(field); //Лишний вызов метода
            Point p = pictureBox1.PointToClient(System.Windows.Forms.Cursor.Position);
            double calcPosX = (p.X / 32) + 1; //координата по оси X
            double calcPosY = (p.Y / 32) + 1; //координата по оси Y

            int posX = Convert.ToInt32(Math.Ceiling(calcPosX));
            int posY = Convert.ToInt32(Math.Ceiling(calcPosY));

            if (ballIsChoosen == false) //Если ни один шарик не выбран
            {
                if (field[posY-1, posX-1] != 0) //Если клетка не пустая
                {
                    ballIsChoosen = true; //Выбираем шар
                    fromX = posX; //Запоминаем позицию выбранного шарика
                    fromY = posY;
                }            
            }
            else
            {
                if (field[posY-1, posX-1] == 0) //Если клетка пустая
                {
                    if (ex.MoveBall(field, fromX, fromY, posX, posY) == true) //Двигаем шар. 
                    {
                        ballIsChoosen = false; //Подвинули - скидываем флаг выбора
                        if (ex.SearchLines(field) == false) //Передаем массив на поиск линий
                        {
                            if (ex.FieldIsFull(field) == false) //Если на поле есть свободные клетки
                                ex.SetupBalls(field); //Если линий не было, ставим новые шары
                            else
                            {
                                Bitmap newBitmap = new Bitmap(pictureBox1.Image); //Затемняем картинку
                                Graphics g = Graphics.FromImage(newBitmap);
                                newBitmap = GrayScale(newBitmap);
                                pictureBox1.Image = newBitmap; //Выводим затемненную картинку
                                pictureBox1.Enabled = false; //Выключаем поле
                                MessageBox.Show("Игра окончена. Вы можете начать новую игру, загрузить игру или выйти.", "Конец игры");
                                return;
                            }
                            ex.SearchLines(field); //И еще раз проверяем на наличие линий
                        }
                    }
                    ballIsChoosen = false; //Если клик по пустой не досягаемой клетке, то скидываем выбранный шар
                }
                else //Если клетка не пустая (клик по другому шарику)
                {
                    ballIsChoosen = true;
                    fromX = posX; //Запоминаем позицию выбранного шарика
                    fromY = posY;
                }
            }
            DrawField(field);
        }

        //Метод рисует игровое поле и шарики в pictureBox//
        private void DrawField(int[,] field)
        {
            //---Кодировка цветов шариков---//
            //0 - пустая клетка-------------//
            //1 - красный(red)--------------//
            //2 - серый(dimgray)------------//
            //3 - желтый(gold)--------------//
            //4 - зеленый(green)------------//
            //5 - голубой(cornflowerblue)---//
            //6 - синий(darkblue)-----------//
            //7 - фиолетовый(purple)--------//
            int sizeOfField = ex.GetSizeOfField() * 32 + 1;
            Bitmap imageOfField = new Bitmap(sizeOfField, sizeOfField); //Создаем пустой рисунок размером с PictureBox
            pictureBox1.Size = new Size(sizeOfField, sizeOfField);
            Graphics GFX = Graphics.FromImage(imageOfField); //Создаем поверхность рисования
            Pen blackPen = new Pen(Color.Black, 1); //Создаем перо шириной в один пиксель

            GFX.FillRectangle(Brushes.White, 0, 0, sizeOfField, sizeOfField); //Закрашиваем поле белым

            int shiftX = 0; //Смещение пера
            int shiftY = 0;

            //Рисуем сетку
            for (int i = 0; i <= ex.GetSizeOfField(); i++)
            {
                GFX.DrawLine(blackPen, shiftX, 0, shiftX, sizeOfField); //Параллельно смещаем точку начала и конца линии по X
                GFX.DrawLine(blackPen, 0, shiftX, sizeOfField, shiftX); //Параллельно смещаем точку начала и конца линии по Y
                shiftX += 32;
            }

            shiftX = 0; //Обнуляем смещения
            shiftY = 0;
            
            //Расставляем шарики
            for (int i = 0; i < ex.GetSizeOfField(); i++)
            {
                for (int j = 0; j < ex.GetSizeOfField(); j++)
                {
                    switch (field[i, j]) //Выбираем цвет
                    { 
                        case 0:
                            break;
                        case 1:
                            GFX.FillEllipse(Brushes.Red, 2 + shiftX, 2 + shiftY, 28, 28);
                            break;
                        case 2:
                            GFX.FillEllipse(Brushes.DimGray, 2 + shiftX, 2 + shiftY, 28, 28);
                            break;
                        case 3:
                            GFX.FillEllipse(Brushes.Gold, 2 + shiftX, 2 + shiftY, 28, 28);
                            break;
                        case 4:
                            GFX.FillEllipse(Brushes.Green, 2 + shiftX, 2 + shiftY, 28, 28);
                            break;
                        case 5:
                            GFX.FillEllipse(Brushes.CornflowerBlue, 2 + shiftX, 2 + shiftY, 28, 28);
                            break;
                        case 6:
                            GFX.FillEllipse(Brushes.DarkBlue, 2 + shiftX, 2 + shiftY, 28, 28);
                            break;
                        case 7:
                            GFX.FillEllipse(Brushes.Purple, 2 + shiftX, 2 + shiftY, 28, 28);
                            break;
                    }
                    shiftX += 32; //Переходим к следущему шарику
                }
                shiftX = 0; //Обнуляем смещение по Х
                shiftY += 32; //Увеличиваем по Y
            }
            lblScore.Text = "Счет: " + ex.GetScore().ToString(); //Выводим счет
            pictureBox1.Image = imageOfField; //Выводим отрисованную картинку в PictureBox.
        }

        //Метод затемняет картинку при проигрыше//
        private Bitmap GrayScale(Bitmap Bmp)
        {
            int rgb;
            Color c;        
            for (int y = 0; y < Bmp.Height; y++)
                for (int x = 0; x < Bmp.Width; x++)
                {
                    c = Bmp.GetPixel(x, y);
                    rgb = (int)((c.R + c.G + c.B) / 3);
                    Bmp.SetPixel(x, y, Color.FromArgb(rgb, rgb, rgb));
                }
            return Bmp;
        }
    }
}
