   M S B A M L   `   `   `  ����E  AUkiConsole, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'clr-namespace:UkiConsole
UkiConsole  5   	       UkiConsole.MainWindow   5   	   \ XPresentationFramework, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089W SPresentationCore, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   *localclr-namespace:UkiConsole     5   	     � Title$  UKI Axis Control��5   	   .��5	      � $��0,0,0,0q�6   .+�5
   
   �� -   5         ��Name$ groupBox��5
      $
��Axes��6%   $��Center=�63   $��0,10,0,0q�6P   $	��Top=�6b   $��850                   ��6z   $	��410��5      .��5      � -   5         �Name$ axGrid��5      $��0,0,0,0q�6#    � RowDefinitions 5      �� 5      �� 5      �� 5      �� 5      �� 5      �� 5      �� 5      �� 5      �� 5       � ColumnDefinitions 5      �� 5      �� 5      �� 5      �� 5      �� 5      �� 5      �� 5       �� 5!      �� 5"      5$      5&      �� 5'   
   -   5        $ 	groupBox1��5'      $��
Input type��6&   $
��Left=�6:   $��45��6U   $��
56,450,0,0q�6a   $	��Top=�6u   $	��166��6�   .��5(      ��  �� Orientation$ 
Horizontal=�6   $��9,0,-9,0q�62   .+�5)      	� -   5         	�Name$ radioButton��5)      $	��CSV��63    d� 	IsChecked$
 TrueY�6A   6R   	� 5*      -   5        $ radioButton1��5*      $	��UDP��64   6B   5+      5,      �� 5-   
   -   5         ��Name$ 
stopbutton��5-      $
��Stop��6%   $
��Left=�64   $��35��6O   $��300,450,0,0q�6[   $	��Top=�6p   $��79��6�   6�   �� 5.   
   -   5        $ button��5.      $
��Show��6!   $
��Left=�60   $��35��6K   $��410,450,0,0q�6W   $	��Top=�6l   $��79��6�   6�   �� 5/   
   -   5        $ button1��5/      $��Clear��6"   $
��Left=�62   $��20��6M   $��500,450,0,0q�6Y   $	��Top=�6n   $��54��6�   6�   �� 50   
   -	   5        $ button2��50      $��	Calibrate��6"   $
��Left=�66   $��19��6Q   $��844,453,0,0q�6]   $	��Top=�6r   $��53��6�   6�   �� 51   
   -
   5        $ buttonConfig��51      $��Config��6'   $
��Left=�68   $��19��6S   $��844,480,0,0q�6_   $	��Top=�6t   $��54��6�   6�   53      54      