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