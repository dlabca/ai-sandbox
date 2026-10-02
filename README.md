# AI Sandbox - low-poly survival sandbox (MonoGame, Android)

Zadne bloky. Teren je hladky low-poly, vsechno je ze skutecnych objektu a kresli se kodem (zadne assety, zadny Content).

## Hlavni napad: stavba z opravdovych klad
- Straum porazis sekerou -> **spadne** (animace) a zustanou po nem **skutecne kladdy lezici na zemi**.
- Kladu **zvednes** (USE), neses (max 4, zpomaluje te), muzes ji **odlozit** (PUT mimo build mod) nebo **vlozit do stroje**.
- Stavis na **skryte mrizce 3 m**, ale vidis jen kladdy: kazda polozena klada = jedna vrstva zdi (srubova stavba, 7 vrstev),
  dvere = preklad na dvou sloupcich, podlaha a strecha po bunkach mrizky. Zdi maji kolize, na podlaze stojis, dvermi projdes.
- Stavba odebiranim vrati kladdy zpet jako skutecne kladdy.

## Postup hrou
1. Seber klacky a kameny ze zeme (USE) -> CRAFT -> **kamenna sekera / krumpac**.
2. Porad stromy (HIT), seber kladdy, postav dum (BUILD, PUT).
3. Krumpacem tez kameny a rudy: **medena a zelezna ruda** (dal od startu).
4. **Pec** (SMELTER, 8 kamenu): vloz rudu + palivo (kura, klacky) -> **medene / zelezne ingoty**.
5. Zelezne nastroje, **GENERATOR** (4 zelezo + 6 med, palivo = kura/klacky) a **ODKORNOVAC** (5 zelezo + 2 med).
   Generator napaji stroje do 22 m. Odkornovac z klady udela **odkorenou kladu** (svetlejsi srub) + kuru (palivo).

## Ovladani (Android)
Levy palec = joystick, pravy palec = rozhlizeni, tlacitka vpravo dole:
HIT (drz) - tezeni/kaceni, JUMP, USE - sebrat/obsluhovat stroj, PUT - polozit, BUILD - prepina WALL/DOOR/FLOOR/ROOF + stroje,
MAT - surove/odkorene kladdy. CRAFT vpravo nahore. V build modu HIT odebira kus stavby.
PC test: WASD, sipky, Space, E=HIT, F=USE, Q=PUT, B=BUILD, R=MAT, C=CRAFT, 1-5 nastroj.

## Build
GitHub Actions (`.github/workflows/build-apk.yml`) sestavi APK a nahraje artefakt `ai-sandbox-apk`.
