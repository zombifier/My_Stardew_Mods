<lane orientation="vertical" horizontal-content-alignment="middle">
  <banner background={@Mods/StardewUI/Sprites/BannerBackground}
      background-border-thickness="48,0"
      padding="12"
      text={HeaderText} />
  <frame layout="880px 640px"
      margin="0,16,0,0"
      padding="32,24"
      background={@Mods/StardewUI/Sprites/ControlBorder}>
    <lane orientation="vertical" horizontal-content-alignment="middle">
      <textinput
        layout="60% 54px"
        text={<>Filter}
        margin="0,0,0,32"
        focusable="true" />
      <scrollable peeking="128">
        <grid layout="stretch content"
           item-layout="length: 64"
           item-spacing="16,16"
           horizontal-item-alignment="middle">
          <frame
            *repeat={FoodItems}
            *if={Visible}
            tooltip={DisplayName}
            click=|^ToggleSelect(Data)|
            border={BorderSprite}
            border-thickness="4"
            focusable="true">
            <image
                layout="stretch content"
                sprite={Data} />
          </frame>
        </grid>
      </scrollable>
    </lane>
  </frame>
  <lane margin="8" orientation="horizontal">
    <button margin="8" text={OrderText} click=|Order()| />
    <button margin="8" text={LuckyText} click=|OrderRandom()| />
  </lane>
</lane>

