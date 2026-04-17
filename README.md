# HeberekeBunnyGardenMod
<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/b5b66725-53ff-4ab2-b495-c9d8f42ad3a3" />

[へべれけ ばにーがーでん](https://store.steampowered.com/app/3575730/_/)(海外名:Merry Bunny Garden)用の解像度修正やフレームレート上限変更などを行うBepInEx5用Modです。

## おしらせ
バージョン1.1.2から、BepInEx6にも対応しました！！  
今までのBepInEx5版もひきつづき開発します！！  
また、下で紹介している導入方法はBepInEx5のものになります！ご了承ください～  

## 対応バージョン(v1.1.2現在)
- ゲームバージョン1.0.1および1.0.2に対応  
- Modバージョン1.0.6まではゲームバージョン 1.0.0のみ対応

## 機能
- 内部解像度を指定することで画質を向上することができる。
- 本来は60で固定されていたフレームレート制限を任意の値にするか、取り払うことができる。
- ダメージを受けない設定、ヒートゲージがたまった状態でも転ばなくなる設定のオンオフができる。
- とある箇所で出てくる謎の光を取り除くことができる。
- F5キーでフリーカメラに切り替えることができ、フリーカメラ中にF6キーを押すとカメラを固定してキャラ操作ができる。

## 導入方法(Steam Deck対応)
1. [Releases](https://github.com/kazumasa200/Hebereke-Bunny-Garden-Fix-Mod/releases)から最新のzipファイルをダウンロードする。(HeberekeBunnyGardenMod v1.0.4.zipみたいな感じ)ブラウザによってはブロックするかもしれないので注意。<br>例<img width="1873" height="898" alt="image" src="https://github.com/user-attachments/assets/d22762a6-3666-4afc-b04d-08c0021018fe" /><br>画像はV1.0.4の例です。導入時の最新バージョンを入れてください。
1. [BepInEx5](https://github.com/bepinex/bepinex/releases)をダウンロードする。Windowsの場合は```BepInEx_win_x64_{バージョン名}.zip```をダウンロードする。
1. ゲームのexeがあるディレクトリにBepInEx5の中身を展開。つまり、HEBEREKE BUNNY GARDEN.exeとBepInExフォルダやdoorstop_configとかが同じ階層にある状態が正しいということ。<br>例<img width="1338" height="390" alt="image" src="https://github.com/user-attachments/assets/c6f690ba-ab66-431c-988f-9f2f6bae5050" />
1. (Steam Deckの場合のみ実行) Steamでバニーガーデン2 → 右クリック → 「プロパティ」→「一般」→「起動オプション」に```WINEDLLOVERRIDES="winhttp=n,b" %command%```を入力。
1. 一度ゲームを起動した後、[Releases](https://github.com/kazumasa200/Hebereke-Bunny-Garden-Fix-Mod/releases)からダウンロードしたZipを展開し、中にある```net.noeleve.HeberekeBunnyGardenMod.dll```をBepinExフォルダの中にPluginsの中に入れる。<br>例<img width="1437" height="280" alt="image" src="https://github.com/user-attachments/assets/4033af59-fa6d-4d72-9dfd-8bf0c914a0fe" />

1. もう一度起動するとBepinExフォルダの中のconfigフォルダに```net.noeleve.HeberekeBunnyGardenMod.cfg```設定ファイルが出来上がるので、それをメモ帳などで変更して解像度の設定や光などの設定をする。<br>例<img width="1509" height="1456" alt="image" src="https://github.com/user-attachments/assets/368ad675-7334-4105-bcb4-3b214e009436" />


## 既知の問題点
[Issues](https://github.com/kazumasa200/Hebereke-Bunny-Garden-Fix-Mod/issues)をご確認ください。バグや改善点、ほしい機能ありましたら[Issues](https://github.com/kazumasa200/Hebereke-Bunny-Garden-Fix-Mod/issues)もしくは[X](https://x.com/kazumasa200)までお願いします。  
要望の際は右上のNew Issueから個別のissueを作ってください。

## 注意
最後に、このModを使用して配信することで動画プラットフォームからBAN等の処置を受けられた際も一切の責任を持ちません。自己責任。

## お問い合わせ
X(旧Twitter):@kazumasa200
