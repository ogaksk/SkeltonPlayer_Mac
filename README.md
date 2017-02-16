# SkeltonPlayer_Mac
kinectのスケルトンデータをmacで再生するプロトタイプ

一人分のトラッキング最低予測
1秒 : 146K
1分 : 8.8M
15分: 135M

6人分だとこれを6倍
最大810M

### mongodb まわりセッティング 

 1.mongoインストール  

 ２.DB作成  
 `$ mongo skeletondb` 
 
 `use skeletondb # mongoコンソールに打ち込む` 

 3.mongoで入ってcollection作成  
 `db.createCollection('skeleton');`

 4.mongoインポート  
 `mongoimport --db skeletondb --collection skeleton --type json  --file  hogehogehoge.json`
