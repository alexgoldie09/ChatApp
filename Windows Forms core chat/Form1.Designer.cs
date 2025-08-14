/*
* Form1.Designer.cs
* ------------------
* This partial class contains the auto-generated UI layout code for the chat application form.
* It defines and initializes all the controls (labels, text boxes, buttons, etc.) that appear
* on the main chat interface, including chat input/output, connection controls, username change,
* and Tic-Tac-Toe buttons. The actual logic and behavior are handled in `Form1.cs`.
*
* This file should generally not be modified manually unless adding new controls or wiring events.
*/

namespace Windows_Forms_Chat
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose(); // Release any UI components created
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new System.Windows.Forms.Label();
            MyPortTextBox = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            serverPortTextBox = new System.Windows.Forms.TextBox();
            label3 = new System.Windows.Forms.Label();
            ServerIPTextBox = new System.Windows.Forms.TextBox();
            ChatTextBox = new System.Windows.Forms.TextBox();
            TypeTextBox = new System.Windows.Forms.TextBox();
            label4 = new System.Windows.Forms.Label();
            HostButton = new System.Windows.Forms.Button();
            JoinButton = new System.Windows.Forms.Button();
            SendButton = new System.Windows.Forms.Button();
            label5 = new System.Windows.Forms.Label();
            button1 = new System.Windows.Forms.Button();
            button2 = new System.Windows.Forms.Button();
            button3 = new System.Windows.Forms.Button();
            button4 = new System.Windows.Forms.Button();
            button5 = new System.Windows.Forms.Button();
            button6 = new System.Windows.Forms.Button();
            button7 = new System.Windows.Forms.Button();
            button8 = new System.Windows.Forms.Button();
            button9 = new System.Windows.Forms.Button();
            StartGameButton = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(13, 13);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(59, 20);
            label1.TabIndex = 0;
            label1.Text = "My Port";
            // 
            // MyPortTextBox
            // 
            MyPortTextBox.Location = new System.Drawing.Point(13, 37);
            MyPortTextBox.Name = "MyPortTextBox";
            MyPortTextBox.Size = new System.Drawing.Size(125, 27);
            MyPortTextBox.TabIndex = 1;
            MyPortTextBox.Text = "6666";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(251, 13);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(80, 20);
            label2.TabIndex = 2;
            label2.Text = "Server Port";
            // 
            // serverPortTextBox
            // 
            serverPortTextBox.Location = new System.Drawing.Point(251, 37);
            serverPortTextBox.Name = "serverPortTextBox";
            serverPortTextBox.Size = new System.Drawing.Size(125, 27);
            serverPortTextBox.TabIndex = 3;
            serverPortTextBox.Text = "6666";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(423, 13);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(64, 20);
            label3.TabIndex = 4;
            label3.Text = "server IP";
            // 
            // ServerIPTextBox
            // 
            ServerIPTextBox.Location = new System.Drawing.Point(423, 37);
            ServerIPTextBox.Name = "ServerIPTextBox";
            ServerIPTextBox.Size = new System.Drawing.Size(159, 27);
            ServerIPTextBox.TabIndex = 5;
            ServerIPTextBox.Text = "127.0.0.1";
            // 
            // ChatTextBox
            // 
            ChatTextBox.Location = new System.Drawing.Point(12, 167);
            ChatTextBox.Multiline = true;
            ChatTextBox.Name = "ChatTextBox";
            ChatTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            ChatTextBox.Size = new System.Drawing.Size(605, 205);
            ChatTextBox.TabIndex = 6;
            ChatTextBox.Text = "\r\n";
            // 
            // TypeTextBox
            // 
            TypeTextBox.Location = new System.Drawing.Point(60, 390);
            TypeTextBox.Name = "TypeTextBox";
            TypeTextBox.Size = new System.Drawing.Size(448, 27);
            TypeTextBox.TabIndex = 7;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(12, 390);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(42, 20);
            label4.TabIndex = 8;
            label4.Text = "Chat:";
            // 
            // HostButton
            // 
            HostButton.Location = new System.Drawing.Point(13, 91);
            HostButton.Name = "HostButton";
            HostButton.Size = new System.Drawing.Size(94, 29);
            HostButton.TabIndex = 9;
            HostButton.Text = "Host Server";
            HostButton.UseVisualStyleBackColor = true;
            HostButton.Click += HostButton_Click;
            // 
            // JoinButton
            // 
            JoinButton.Location = new System.Drawing.Point(251, 91);
            JoinButton.Name = "JoinButton";
            JoinButton.Size = new System.Drawing.Size(94, 29);
            JoinButton.TabIndex = 10;
            JoinButton.Text = "Join Server";
            JoinButton.UseVisualStyleBackColor = true;
            JoinButton.Click += JoinButton_Click;
            // 
            // SendButton
            // 
            SendButton.Location = new System.Drawing.Point(523, 390);
            SendButton.Name = "SendButton";
            SendButton.Size = new System.Drawing.Size(94, 29);
            SendButton.TabIndex = 11;
            SendButton.Text = "Send";
            SendButton.UseVisualStyleBackColor = true;
            SendButton.Click += SendButton_Click;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(173, 34);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(29, 20);
            label5.TabIndex = 12;
            label5.Text = "OR";
            // 
            // button1
            // 
            button1.BackColor = System.Drawing.Color.Gray;
            button1.Font = new System.Drawing.Font("Segoe UI", 19F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            button1.Location = new System.Drawing.Point(731, 34);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(94, 98);
            button1.TabIndex = 13;
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.BackColor = System.Drawing.Color.Gray;
            button2.Font = new System.Drawing.Font("Segoe UI", 19F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            button2.Location = new System.Drawing.Point(831, 34);
            button2.Name = "button2";
            button2.Size = new System.Drawing.Size(94, 98);
            button2.TabIndex = 13;
            button2.UseVisualStyleBackColor = false;
            button2.Click += button2_Click;
            // 
            // button3
            // 
            button3.BackColor = System.Drawing.Color.Gray;
            button3.Font = new System.Drawing.Font("Segoe UI", 19F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            button3.Location = new System.Drawing.Point(931, 34);
            button3.Name = "button3";
            button3.Size = new System.Drawing.Size(94, 98);
            button3.TabIndex = 13;
            button3.UseVisualStyleBackColor = false;
            button3.Click += button3_Click;
            // 
            // button4
            // 
            button4.BackColor = System.Drawing.Color.Gray;
            button4.Font = new System.Drawing.Font("Segoe UI", 19F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            button4.Location = new System.Drawing.Point(731, 138);
            button4.Name = "button4";
            button4.Size = new System.Drawing.Size(94, 98);
            button4.TabIndex = 13;
            button4.UseVisualStyleBackColor = false;
            button4.Click += button4_Click;
            // 
            // button5
            // 
            button5.BackColor = System.Drawing.Color.Gray;
            button5.Font = new System.Drawing.Font("Segoe UI", 19F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            button5.Location = new System.Drawing.Point(831, 138);
            button5.Name = "button5";
            button5.Size = new System.Drawing.Size(94, 98);
            button5.TabIndex = 13;
            button5.UseVisualStyleBackColor = false;
            button5.Click += button5_Click;
            // 
            // button6
            // 
            button6.BackColor = System.Drawing.Color.Gray;
            button6.Font = new System.Drawing.Font("Segoe UI", 19F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            button6.Location = new System.Drawing.Point(931, 138);
            button6.Name = "button6";
            button6.Size = new System.Drawing.Size(94, 98);
            button6.TabIndex = 13;
            button6.UseVisualStyleBackColor = false;
            button6.Click += button6_Click;
            // 
            // button7
            // 
            button7.BackColor = System.Drawing.Color.Gray;
            button7.Font = new System.Drawing.Font("Segoe UI", 19F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            button7.Location = new System.Drawing.Point(731, 242);
            button7.Name = "button7";
            button7.Size = new System.Drawing.Size(94, 98);
            button7.TabIndex = 13;
            button7.UseVisualStyleBackColor = false;
            button7.Click += button7_Click;
            // 
            // button8
            // 
            button8.BackColor = System.Drawing.Color.Gray;
            button8.Font = new System.Drawing.Font("Segoe UI", 19F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            button8.Location = new System.Drawing.Point(831, 242);
            button8.Name = "button8";
            button8.Size = new System.Drawing.Size(94, 98);
            button8.TabIndex = 13;
            button8.UseVisualStyleBackColor = false;
            button8.Click += button8_Click;
            // 
            // button9
            // 
            button9.BackColor = System.Drawing.Color.Gray;
            button9.Font = new System.Drawing.Font("Segoe UI", 19F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            button9.Location = new System.Drawing.Point(931, 242);
            button9.Name = "button9";
            button9.Size = new System.Drawing.Size(94, 98);
            button9.TabIndex = 13;
            button9.UseVisualStyleBackColor = false;
            button9.Click += button9_Click;
            // 
            // StartGameButton
            // 
            StartGameButton.Enabled = false;
            StartGameButton.Location = new System.Drawing.Point(831, 360);
            StartGameButton.Name = "StartGameButton";
            StartGameButton.Size = new System.Drawing.Size(94, 29);
            StartGameButton.TabIndex = 14;
            StartGameButton.Text = "Start Game";
            StartGameButton.UseVisualStyleBackColor = true;
            StartGameButton.Click += StartGameButton_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1153, 509);
            Controls.Add(StartGameButton);
            Controls.Add(button9);
            Controls.Add(button8);
            Controls.Add(button7);
            Controls.Add(button6);
            Controls.Add(button5);
            Controls.Add(button4);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(label5);
            Controls.Add(SendButton);
            Controls.Add(JoinButton);
            Controls.Add(HostButton);
            Controls.Add(label4);
            Controls.Add(TypeTextBox);
            Controls.Add(ChatTextBox);
            Controls.Add(ServerIPTextBox);
            Controls.Add(label3);
            Controls.Add(serverPortTextBox);
            Controls.Add(label2);
            Controls.Add(MyPortTextBox);
            Controls.Add(label1);
            Name = "Form1";
            Text = "ChatApp";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        // UI Components declarations
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox MyPortTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox serverPortTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox ServerIPTextBox;
        public System.Windows.Forms.TextBox ChatTextBox;
        private System.Windows.Forms.TextBox TypeTextBox;
        private System.Windows.Forms.Label label4;
        public System.Windows.Forms.Button HostButton;
        public System.Windows.Forms.Button JoinButton;
        public System.Windows.Forms.Button SendButton;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.Button button8;
        private System.Windows.Forms.Button button9;
        public System.Windows.Forms.Button StartGameButton;
    }
}

