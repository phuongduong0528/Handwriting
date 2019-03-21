using Accord.Math;
using Accord.Neuro;
using Accord.Neuro.Learning;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Handwriting
{
    public partial class Form1 : Form
    {
        ActivationNetwork network;
        ParallelResilientBackpropagationLearning teacherBL;

        public Form1()
        {
            InitializeComponent();
            drawingBoard.PenSize = 4;
        }

        private void BtnClearCanvas_Click(object sender, EventArgs e)
        {
            drawingBoard.Clear();
            lblResult.Text = "-";
        }

        private void BtnLoadData_Click(object sender, EventArgs e)
        {
            network = new ActivationNetwork(new SigmoidFunction(2), 1024, 256, 128, 10);
            new NguyenWidrow(network).Randomize();

            StringReader reader = new StringReader(File.ReadAllText("Data\\optdigits-tra.txt"));
            int trainingStart = 0;
            int trainingCount = 1000;

            int testingStart = 1000;
            int testingCount = 1000;

            int c1 = 0;
            int c2 = 1;
            int trainingSet = 0;
            int testingSet = 0;

            dgvLearnSet.Rows.Clear();
            dgvLearnSet.RowTemplate.Height = 40;

            dgvVerifySet.Rows.Clear();
            dgvVerifySet.RowTemplate.Height = 40;

            while (true)
            {
                char[] buffer = new char[33 * 32];
                int read = reader.ReadBlock(buffer, 0, buffer.Length);
                string label = reader.ReadLine();

                if (read < buffer.Length || label == null)
                    break;

                if (c1 > trainingStart && c1 <= trainingStart + trainingCount)
                {
                    Bitmap bitmap = Ultilities.Extract(new string(buffer));
                    double[] features = Ultilities.Extract(bitmap);
                    int clabel1 = Int32.Parse(label);
                    dgvLearnSet.Rows.Add(c1, bitmap, clabel1, features);
                    trainingSet++;

                }
                else if (c1 > testingStart && c1 <= testingStart + testingCount)
                {
                    Bitmap bitmap = Ultilities.Extract(new string(buffer));
                    double[] features = Ultilities.Extract(bitmap);
                    int clabel1 = Int32.Parse(label);
                    dgvVerifySet.Rows.Add(c2, bitmap, "", features, clabel1);
                    testingSet++;
                    c2++;
                }
                c1++;
            }
            btnLearn.Enabled = true;
            btnVerify.Enabled = true;
        }

        private double[] Out(int value)
        {
            double[] re = new double[10] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
            re[value] = 1.0;
            return re;
        }

        private void BtnLearn_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Hãy chờ vài phút!";
            lblStatus.ForeColor = Color.Red;
            lblStatus.Visible = true;
            int rows = dgvLearnSet.Rows.Count;
            double[][] input = new double[rows - 1][];
            double[][] output = new double[rows - 1][];
            teacherBL = new ParallelResilientBackpropagationLearning(network);
            double error = int.MaxValue;
            double lbl;
            for (int i = 0; i < rows - 1; i++)
            {
                input[i] = (double[])dgvLearnSet.Rows[i].Cells[3].Value;
                lbl = Convert.ToDouble(dgvLearnSet.Rows[i].Cells[2].Value);
                output[i] = Out(Convert.ToInt32(dgvLearnSet.Rows[i].Cells[2].Value));
            }
            while (true)
            {
                error = teacherBL.RunEpoch(input, output);
                if (error < 0.1)
                    break;
            }
            btnVerify.Enabled = true;
            btnClassify.Enabled = true;
            lblStatus.Text = "Đã huấn luyện xong!";
            lblStatus.ForeColor = Color.DarkGreen;
        }

        private void BtnVerify_Click(object sender, EventArgs e)
        {
            int correctCount = 0;
            foreach (DataGridViewRow dgvRows in dgvVerifySet.Rows)
            {
                if (dgvRows.Cells[2].Value != null)
                {
                    double[] input = (double[])dgvRows.Cells[3].Value;
                    int expected = (int)dgvRows.Cells[4].Value;

                    double[] netOutput = network.Compute(input);
                    int predicted = netOutput.ToList().IndexOf(netOutput.Max());
                    dgvRows.Cells[2].Value = predicted;
                    if (predicted == expected)
                    {
                        correctCount++;
                        dgvRows.Cells[0].Style.BackColor = Color.LightBlue;
                        dgvRows.Cells[1].Style.BackColor = Color.LightBlue;
                        dgvRows.Cells[2].Style.BackColor = Color.LightBlue;
                        dgvRows.Cells[4].Style.BackColor = Color.LightBlue;
                    }
                    else
                    {
                        dgvRows.Cells[0].Style.BackColor = Color.LightCoral;
                        dgvRows.Cells[1].Style.BackColor = Color.LightCoral;
                        dgvRows.Cells[2].Style.BackColor = Color.LightCoral;
                        dgvRows.Cells[4].Style.BackColor = Color.LightCoral;
                    }
                }
            }
            double percent = ((double)correctCount / (double)(dgvVerifySet.RowCount - 1.0)) * 100;
            MessageBox.Show($"Độ chính xác {Math.Round(percent, 1)}% ({correctCount}/{dgvVerifySet.RowCount - 1})", "",
                           MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnClassify_Click(object sender, EventArgs e)
        {
            try
            {
                double[] input = drawingBoard.GetDigit();
                double[] predicted = network.Compute(input);
                int result = predicted.ToList().IndexOf(predicted.Max());
                lblResult.Text = $"{result}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hãy huấn luyện trước!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                BtnClearCanvas_Click(this, EventArgs.Empty);
            }
        }

        private void DrawingBoard_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                BtnClassify_Click(this, EventArgs.Empty);

        }
    }
}
