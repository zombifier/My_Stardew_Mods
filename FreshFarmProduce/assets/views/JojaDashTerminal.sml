<lane orientation="vertical" horizontal-content-alignment="middle">
  <banner background={@Mods/StardewUI/Sprites/BannerBackground}
      background-border-thickness="48,0"
      padding="12"
      text={HeaderText} />
  <frame layout="880px 640px"
      margin="0,16,0,0"
      padding="32,24"
      pointer-leave=|OnUnhover()|
      background={@Mods/StardewUI/Sprites/ControlBorder}>
    <lane orientation="vertical" horizontal-content-alignment="middle">
      <textinput
        layout="60% 54px"
        text={<>Filter}
        margin="0,0,0,8"
        focusable="true" />
      <image
        fit="stretch"
        layout="100% 10px"
        margin="0,0,0,8"
        sprite={@Mods/StardewUI/Sprites/GenericHorizontalDivider} />
      <scrollable peeking="128">
        <grid layout="stretch content"
           item-layout="length: 96"
           pointer-leave=|OnUnhover()|
           horizontal-item-alignment="middle">
          <panel
            horizontal-content-alignment="middle"
            vertical-content-alignment="middle"
            layout="96px"
            *repeat={FoodItems}
            *if={Visible}
            click=|^ToggleSelect(this)|
            pointer-enter=|^OnHover(this)|
            pointer-leave=|^OnUnhover()|
            focusable="true">
            <image
              layout="96px"
              *if={Selected}
              sprite={@Mods/StardewUI/Sprites/ControlBorder} />
            <image
              layout="64px"
              sprite={Data} />
          </panel>
        </grid>
      </scrollable>
    </lane>
  </frame>
  <lane margin="8" orientation="horizontal">
    <button margin="8" text={OrderText} click=|Order()| tooltip={OrderTooltip} />
    <button margin="8" text={LuckyText} click=|OrderRandom()| tooltip={LuckyTooltip} />
  </lane>
</lane>

