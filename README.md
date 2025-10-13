# HeberekeBunnyGardenMod
<img width="3840" height="2160" alt="image" src="https://github.com/user-attachments/assets/b5b66725-53ff-4ab2-b495-c9d8f42ad3a3" />

[へべれけ ばにーがーでん](https://store.steampowered.com/app/3575730/_/)(海外名:Merry Bunny Garden)用の解像度修正やフレームレート上限変更などを行うBepInEx5用Modです。

## 機能
- 内部解像度を指定することで画質を向上することができる。
- 本来は60で固定されていたフレームレート制限を任意の値にするか、取り払うことができる。
- ダメージを受けない設定、ヒートゲージがたまった状態でも転ばなくなる設定のオンオフができる。
- とある箇所で出てくる怪しい光を取り除くことができる。

## 導入方法
1. Releasesから最新のzipファイルをダウンロードする。(HeberekeBunnyGardenMod v1.0.4.zipみたいな感じ)ブラウザによってはブロックするかもしれないので注意。<br>例<img width="1873" height="898" alt="image" src="https://github.com/user-attachments/assets/d22762a6-3666-4afc-b04d-08c0021018fe" /><br>画像はV1.0.4の例です。導入時の最新バージョンを入れてください。
1. [BepInEx5](https://github.com/bepinex/bepinex/releases)をダウンロードする。Windowsの場合は```BepInEx_win_x64_{バージョン名}.zip```をダウンロードする。
1. ゲームのexeがあるディレクトリにBepInEx5の中身を展開。つまり、HEBEREKE BUNNY GARDEN.exeとBepInExフォルダやdoorstop_configとかが同じ階層にある状態が正しいということ。<br>例<img width="1338" height="390" alt="image" src="https://github.com/user-attachments/assets/c6f690ba-ab66-431c-988f-9f2f6bae5050" />
1. 一度ゲームを起動した後、ReleasesからダウンロードしたZipを展開し、中にある```net.noeleve.HeberekeBunnyGardenMod.dll```をBepinExフォルダの中にPluginsの中に入れる。<br>例<img width="1437" height="280" alt="image" src="https://github.com/user-attachments/assets/4033af59-fa6d-4d72-9dfd-8bf0c914a0fe" />

1. もう一度起動するとBepinExフォルダの中のconfigフォルダに```net.noeleve.HeberekeBunnyGardenMod.cfg```設定ファイルが出来上がるので、それをメモ帳などで変更して解像度の設定や光などの設定をする。<br>例<img width="1509" height="1456" alt="image" src="https://github.com/user-attachments/assets/368ad675-7334-4105-bcb4-3b214e009436" />


## 既知の問題点
Issueをご確認ください。バグや改善点、ほしい機能ありましたらIssueまでお願いします。

## 注意
最後に、このModを使用して配信することで動画プラットフォームからBAN等の処置を受けられた際も一切の責任を持ちません。自己責任。

## おしらせ
バージョン1.0.3からフレームレート制限の実装法を見直し、128FPS以上を設定できるようになりました！  
-1を設定することで無限にも!?  
チート系の設定もありますが、クリアしてグランドエンディングを見るまではfalseにしておくことを強く推奨します。

## お問い合わせ
X(旧Twitter):@kazumasa200
