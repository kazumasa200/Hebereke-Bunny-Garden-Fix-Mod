# HeberekeBunnyGardenMod
<img width="3840" height="2160" alt="image" src="https://github.com/user-attachments/assets/b5b66725-53ff-4ab2-b495-c9d8f42ad3a3" />

[へべれけ ばにーがーでん](https://store.steampowered.com/app/3575730/_/)(海外名:Merry Bunny Garden)用の解像度修正やフレームレート上限変更などを行うBepInEx5用Modです。

## 機能
- 内部解像度を指定することで画質を向上することができる。
- 本来は60で固定されていたフレームレート制限を任意の値にするか、取り払うことができる。
- ダメージを受けない設定、ヒートゲージがたまった状態でも転ばなくなる設定のオンオフができる。
- とある箇所で出てくる怪しい光を取り除くことができる。

## 導入方法
1. Releasesから最新のzipファイルをダウンロードする。(HeberekeBunnyGardenMod v....zipみたいな感じ)ブラウザによってはブロックするかもしれないので注意。
1. [BepInEx5](https://github.com/bepinex/bepinex/releases)をダウンロードする。Windowsの場合はBepInEx_win_x64_5.*.*.*.zip系をダウンロードする。
1. ゲームのexeがあるディレクトリに中身を展開。つまり、HEBEREKE BUNNY GARDEN.exeとBepInExフォルダやdoorstop_configとかが同じ階層にある状態が正しいということ。
1. 一度ゲームを起動した後、ReleasesからダウンロードしたZipを展開し、中にあるファイルをBepinExフォルダの中にPluginsの中にぶち込む。
1. もう一度起動するとBepinExフォルダの中のconfigフォルダに設定ファイルが出来上がるので、それをいじる。

## 既知の問題点
Issueをご確認ください。バグや改善点、ほしい機能ありましたらIssueまでお願いします。

## 注意
最後に、このModを使用して配信することで動画プラットフォームからBAN等の処置を受けられた際も一切の責任を持ちません。自己責任。

## おしらせ
バージョン1.0.3からフレームレート制限の実装法を見直し、128FPS以上を設定できるようになりました！  
-1を設定することで無限にも!?  
チート系の設定もありますが、クリアしてグランドエンディングを見るまではfalseにしておくことを強く推奨します。
