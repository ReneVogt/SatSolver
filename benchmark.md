# Fixed Clause Minimization
- fixing outer loop condition and removing unnecessary checks
- same options as baseline

| Method | Case             | Mean          | Error       | StdDev      | Median        |
|------- |----------------- |--------------:|------------:|------------:|--------------:|
| Solve  | 2bitadd_10       |  5,091.053 ms |  34.3658 ms |  65.3845 ms |  5,072.943 ms |
| Solve  | 2bitadd_11       |      2.978 ms |   0.0362 ms |   0.0689 ms |      2.936 ms |
| Solve  | 3blocks          |     10.776 ms |   0.1614 ms |   0.3070 ms |     10.659 ms |
| Solve  | bmc-galileo-9    | 28,384.377 ms | 296.5453 ms | 612.4175 ms | 28,266.345 ms |
| Solve  | e0ddr2-10-by-5-1 |  3,180.255 ms |  36.9149 ms | 112.2582 ms |  3,162.862 ms |
| Solve  | qg3-09           |  5,926.361 ms |  38.6150 ms |  73.4690 ms |  5,919.704 ms |
| Solve  | qg5-13           |  4,095.232 ms |  33.9652 ms |  65.4395 ms |  4,094.768 ms |
| Solve  | random3          |  8,809.509 ms |  56.5942 ms | 107.6764 ms |  8,793.018 ms |
| Solve  | random3u         |  8,277.434 ms |  26.0116 ms |  49.4898 ms |  8,279.700 ms |

# Baseline
- No restarts
- MaxLBD: 100
- CD: 5000 Conflicts, CF 4, LBD 3, R 0.5

| Method | Case             | Mean          | Error       | StdDev      |
|------- |----------------- |--------------:|------------:|------------:|
| Solve  | 2bitadd_10       |  6,652.107 ms |  45.2562 ms |  86.1046 ms |
| Solve  | 2bitadd_11       |      2.982 ms |   0.0248 ms |   0.0472 ms |
| Solve  | 3blocks          |     21.988 ms |   0.1947 ms |   0.3705 ms |
| Solve  | bmc-galileo-9    | 28,462.804 ms | 323.9183 ms | 711.0087 ms |
| Solve  | e0ddr2-10-by-5-1 |  3,799.930 ms |  39.8549 ms | 107.0676 ms |
| Solve  | qg3-09           |  2,681.715 ms |  13.6798 ms |  26.0272 ms |
| Solve  | qg5-13           |  3,727.810 ms |  30.6851 ms |  58.3816 ms |
| Solve  | random3          |  9,219.572 ms |  53.6662 ms | 102.1055 ms |
| Solve  | random3u         | 13,761.625 ms |  81.5186 ms | 155.0976 ms |