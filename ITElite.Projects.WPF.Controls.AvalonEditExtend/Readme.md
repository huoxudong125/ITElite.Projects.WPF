##Demo Usage
```xml
<avalonedit:TextEditor  Grid.Row="1" Grid.Column="0"
                                HorizontalAlignment="Stretch"
                  VerticalAlignment="Stretch"
                  HorizontalScrollBarVisibility="Auto"
                  VerticalScrollBarVisibility="Auto"
                  Margin="0 0 0 5"
                  FontFamily="Consolas"
                    SyntaxHighlighting="XML"
                    FontSize="10pt">    
            <i:Interaction.Behaviors>
                <local:AvalonEditBehaviour 
GiveMeTheText="{Binding XmlContent, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
            </i:Interaction.Behaviors>
        </avalonedit:TextEditor>
```