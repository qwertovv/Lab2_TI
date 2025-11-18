using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace WPCalculator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string code = txtCode.Text;
                string postCondition = txtPostCondition.Text;

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(postCondition))
                {
                    MessageBox.Show("Пожалуйста, введите код и постусловие");
                    return;
                }

                var calculator = new WeakestPreconditionCalculator();
                var result = calculator.CalculateWP(code, postCondition);

                DisplayResults(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void DisplayResults(WPResult result)
        {
            StringBuilder traceBuilder = new StringBuilder();
            foreach (var step in result.Steps)
            {
                traceBuilder.AppendLine(step);
            }
            txtTrace.Text = traceBuilder.ToString();

            txtWPResult.Text = $"Слабейшее предусловие: {result.FinalPrecondition}";
            txtWPDescription.Text = result.FinalDescription;
        }

        private void BtnShowTriad_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtWPResult.Text) && !string.IsNullOrEmpty(txtPostDescription.Text))
            {
                string pre = txtWPResult.Text.Replace("Слабейшее предусловие: ", "");
                string post = txtPostDescription.Text;
                string code = txtCode.Text;

                MessageBox.Show($"{{ {pre} }}\n{code}\n{{ {post} }}", "Триада Хоара");
            }
            else
            {
                MessageBox.Show("Сначала рассчитайте слабейшее предусловие", "Информация");
            }
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = new HelpWindow();
            helpWindow.Owner = this;
            helpWindow.ShowDialog();
        }

        private void BtnPreset1_Click(object sender, RoutedEventArgs e)
        {
            txtCode.Text = "if (x1 >= x2) max := x1; else max := x2;";
            txtPostCondition.Text = "max > 100";
            txtPostDescription.Text = "Максимальное значение больше 100";
        }

        private void BtnPreset2_Click(object sender, RoutedEventArgs e)
        {
            txtCode.Text = @"if (D >= 0) 
    root := (-b + sqrt(D)) / (2*a);
else 
    root := -999;";
            txtPostCondition.Text = "root >= -999";
            txtPostDescription.Text = "Корень либо действительный, либо специальное значение";
        }

        private void BtnPreset3_Click(object sender, RoutedEventArgs e)
        {
            txtCode.Text = @"x := x + 10;
y := x + 1;";
            txtPostCondition.Text = "y == x - 9 && x > 15";
            txtPostDescription.Text = "y равно x-9 и x больше 15";
        }
    }

    public class WPResult
    {
        public List<string> Steps { get; set; } = new List<string>();
        public string FinalPrecondition { get; set; } = "";
        public string FinalDescription { get; set; } = "";
    }

    public class WeakestPreconditionCalculator
    {
        public WPResult CalculateWP(string code, string postCondition)
        {
            var result = new WPResult();
            result.Steps.Add($"Начальное постусловие: {postCondition}");

            string[] lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string currentCondition = postCondition;

            for (int i = lines.Length - 1; i >= 0; i--)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                result.Steps.Add($"Обрабатываем: {line}");

                if (line.StartsWith("if"))
                {
                    currentCondition = ProcessIfStatement(line, currentCondition, result);
                }
                else if (line.Contains(":="))
                {
                    currentCondition = ProcessAssignment(line, currentCondition, result);
                }
                else
                {
                    currentCondition = ProcessSequence(line, currentCondition, result);
                }

                result.Steps.Add($"Текущее условие: {currentCondition}");
                result.Steps.Add("");
            }

            result.FinalPrecondition = currentCondition;
            result.FinalDescription = $"Гарантирует, что {postCondition} после выполнения программы";

            return result;
        }

        private string ProcessAssignment(string line, string postCondition, WPResult result)
        {
            var parts = line.Split(new[] { ":=" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                throw new ArgumentException($"Некорректное присваивание: {line}");
            }

            string variable = parts[0].Trim();
            string expression = parts[1].Trim().TrimEnd(';');

            string newCondition = ReplaceVariable(postCondition, variable, expression);

            string definedConditions = AddDefinednessConditions(expression);
            if (!string.IsNullOrEmpty(definedConditions))
            {
                newCondition = $"{definedConditions} && {newCondition}";
            }

            result.Steps.Add($"WP присваивания: заменяем '{variable}' на '{expression}'");
            result.Steps.Add($"Условия определенности: {definedConditions}");

            return newCondition;
        }

        private string ProcessIfStatement(string line, string postCondition, WPResult result)
        {
            int thenIndex = line.IndexOf(")");
            int elseIndex = line.IndexOf("else");

            if (thenIndex == -1 || elseIndex == -1)
            {
                throw new ArgumentException($"Некорректный оператор if: {line}");
            }

            string condition = line.Substring(line.IndexOf("(") + 1, thenIndex - line.IndexOf("(") - 1).Trim();

            string wp = $"({condition} && wp(ветка then, {postCondition})) || (!{condition} && wp(ветка else, {postCondition}))";

            result.Steps.Add($"WP оператора if: ({condition} ∧ wp(then)) ∨ (¬{condition} ∧ wp(else))");

            return wp;
        }

        private string ProcessSequence(string line, string postCondition, WPResult result)
        {
            return postCondition;
        }

        private string ReplaceVariable(string condition, string variable, string expression)
        {
            return condition.Replace(variable, $"({expression})");
        }

        private string AddDefinednessConditions(string expression)
        {
            List<string> conditions = new List<string>();

            if (expression.Contains("/"))
            {
                conditions.Add("знаменатель != 0");
            }

            if (expression.Contains("sqrt"))
            {
                conditions.Add("выражение_под_корнем >= 0");
            }

            return string.Join(" && ", conditions);
        }
    }
}