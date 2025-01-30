# Scanned Card Denoiser
## 개요
![ScannedCardDenoiser](https://github.com/user-attachments/assets/dba2763b-ab8b-4e48-aa73-fa0c912b5ed9)

**Scanned Card Denoiser는 스캔한 트레이딩 카드 및 카드다스 이미지를 몇개의 파라미터 조정으로 대량의 이미지를 손쉽게 보정하는 목적으로 개발하는 프로그램입니다.**

| Source | Result |
| :---: | :---: |
| ![11100010](https://github.com/user-attachments/assets/5c0bfa50-e7ca-45bf-b259-4ad971968e01) | ![11100010](https://github.com/user-attachments/assets/682c976a-2eb4-41f6-8a96-0b84633ba84b) |
| 1024 x 704 | 860 x 590 |

## 매뉴얼
### 1. 이미지 보정
* Auto Level Image : 이미지의 톤 보정을 수행합니다.

| Source | Result |
| :---: | :---: |
| ![11200780](https://github.com/user-attachments/assets/782132b5-5940-42aa-88bf-793322e10187) | ![11200780](https://github.com/user-attachments/assets/f024d5e8-fe9b-4e5a-a5ae-dbccc92af235) |

* Non-local Means Denoise : 스캔과정에서 포함된 먼지와 같은 노이즈를 줄입니다.

| Source | Result |
| :---: | :---: |
| ![Test1](https://github.com/user-attachments/assets/53aa5ede-e056-4a47-906c-8af897c2e2a4) | ![Test1](https://github.com/user-attachments/assets/4cf38ec7-b8c5-4bd3-86ad-4e9d50f4c6f9) |

* Auto Adjustment : 이미지 수평을 맞추고 여백을 제거합니다.
  
| Source | Result |
| :---: | :---: |
| ![Test1](https://github.com/user-attachments/assets/a5753f65-2e1f-40d8-968a-18a0828662ce) | ![Test1](https://github.com/user-attachments/assets/f4c49df2-bb69-42dc-a334-f5df63db153e) |

### 2. 이미지 편집
* 사이즈 변경 : 최종 이미지 크기를 결정합니다.
* 모서리 자르기 : 최종 이미지의 모서리를 자릅니다.
* 테두리 그리기 : 최종 이미지의 테두리를 검은 픽셀로 채웁니다.
* 코너 라운딩 : 최종 이미지의 코너를 라운딩화합니다.

### 3. waifu2x
* Model : waifu2x에 사용될 AI 모델을 선택합니다.
* Denoise Level : waifu2x의 결과에 반영할 디노이즈 레벨을 선택합니다.

| waifu2x off | waifu2x on |
| :---: | :---: |
| ![50500010](https://github.com/user-attachments/assets/011d5b31-b591-40b2-b54e-fa50fec45ce1) | ![50500010](https://github.com/user-attachments/assets/82e929ce-f716-4193-9ba7-2223bd6b5d02) |

**참고 : 내부에서 [waifu2x-ncnn-vulkan](https://github.com/nihui/waifu2x-ncnn-vulkan)을 호출하여 사용합니다.**
