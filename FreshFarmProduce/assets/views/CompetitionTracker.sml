<lane orientation="vertical" horizontal-content-alignment="middle">
  <banner background={@Mods/StardewUI/Sprites/BannerBackground}
      background-border-thickness="48,0"
      padding="16"
      margin="0,0,0,32"
      text={:HeaderText} />
  <frame layout="880px 640px"
      margin="0,0,0,0"
      padding="32,24"
      background={@Mods/StardewUI/Sprites/ControlBorder}>
    <scrollable peeking="128">
      <lane orientation="vertical"
         vertical-content-alignment="middle">
        <expander
          *repeat={:Objectives} >
          <lane
            *outlet="header"
            focusable="true"
            orientation ="horizontal"
            margin="8">
            <frame
              padding="8"
              vertical-content-alignment="middle"
              background={@Mods/StardewUI/Sprites/ControlBorder}>
            <image
              layout="64px 64px"
              padding="4,4,4,4"
              sprite={:Sprite}/>
            </frame>
            <lane
              padding="16,0,16,8"
              layout="stretch"
              orientation="vertical" >
              <lane
                orientation="horizontal">
                <banner text={:Name} margin="0,4,0,0"/>
                <spacer layout="stretch 0px" />
                <label text={:Points}
                  font="small"
                  padding="0,24,0,0"
                  color={:TextColor}
                />
                <label text="/" font="small" padding="0,24,0,0" color={:TextColor}/>
                <label text={:TotalPoints} font="small" padding="0,24,0,0" color={:TextColor}/>
              </lane>
              <panel
                layout="stretch content" >
                <image
                  layout="stretch stretch"
                  fit="stretch"
                  sprite={@Mods/selph.FreshFarmProduce/Sprites/progress_bar:ProgressBar} />
                <image
                  layout={:BarPercentage}
                  margin="4"
                  tint={:BarColor}
                  fit="stretch"
                  sprite={:BarTexture} />
              </panel>
            </lane>
          </lane>
          <lane
            focusable="true"
            margin="0,0,0,16"
            orientation="vertical">
            <label
              margin="0,8,0,16"
              layout="stretch content"
              font="dialogue"
              text={:Description} />
            <lane
              orientation="horizontal"
              margin="24,0,24,0"
              *repeat={:Entries}>
              <image
                layout="32px 32px"
                sprite={:Data}/>
              <label margin="4,0,0,0" text={:ItemName} />
              <spacer layout="stretch 0px" />
              <label text={:Points} font="small"/>
              <label text="/" font="small" *if={:HasThreshold}/>
              <label text={:TotalPoints} font="small" *if={:HasThreshold}/>
            </lane>
          </lane>
        </expander>
      </lane>
    </scrollable>
  </frame>
  <lane
    vertical-content-alignment="end"
    layout="940px content">
    <frame
      padding="16"
      background={@Mods/StardewUI/Sprites/ControlBorder}>
        <label text={:PresetName} tooltip={:PresetDescription} focusable="true"/>
    </frame>
    <spacer layout = "stretch 0px"/>
    <frame
      padding="16"
      background={@Mods/StardewUI/Sprites/ControlBorder}>
        <label text={:FameBanner} tooltip={:FameBannerTooltip} focusable="true"/>
    </frame>
  </lane>
</lane>

