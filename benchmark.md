# Fixed Clause Minimization
- fixing outer loop condition
- same options as baseline

| Method | Case             | Mean          | Error         | StdDev        | Median        |
|------- |----------------- |--------------:|--------------:|--------------:|--------------:|
| Solve  | 2bitadd_10       |  6,231.354 ms | 2,924.1561 ms | 9,283.2447 ms |  5,027.200 ms |
| Solve  | 2bitadd_11       |      2.967 ms |     0.0198 ms |     0.0377 ms |      2.957 ms |
| Solve  | 3blocks          |     10.829 ms |     0.0952 ms |     0.1812 ms |     10.780 ms |
| Solve  | bmc-galileo-9    | 28,688.448 ms |   310.2266 ms |   778.2980 ms | 28,704.746 ms |
| Solve  | e0ddr2-10-by-5-1 |  3,138.073 ms |    36.1824 ms |   111.1236 ms |  3,104.216 ms |
| Solve  | qg3-09           |  6,047.402 ms |    93.3486 ms |   179.8510 ms |  5,951.553 ms |
| Solve  | qg5-13           |  4,071.572 ms |    29.4940 ms |    56.1155 ms |  4,063.184 ms |
| Solve  | random3          |  8,963.567 ms |    45.2207 ms |    86.0370 ms |  8,946.665 ms |
| Solve  | random3u         |  8,346.269 ms |    45.0411 ms |    85.6954 ms |  8,326.646 ms |

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