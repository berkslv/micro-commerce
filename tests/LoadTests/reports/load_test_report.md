> test info



test suite: `nbomber_default_test_suite_name`

test name: `nbomber_default_test_name`

session id: `2026-01-04_18.10.37_session_72d27bb3`

> scenario stats



scenario: `normal_load_with_orders`

  - ok count: `2447`

  - fail count: `474`

  - all data: `3.7` MB

  - duration: `00:00:46`

load simulations:

  - `inject`, rate: `100`, interval: `00:00:01`, during: `00:02:00`

|step|ok stats|
|---|---|
|name|`global information`|
|request count|all = `2921`, ok = `2447`, RPS = `53.2`|
|latency (ms)|min = `6.83`, mean = `8153.99`, max = `20484.93`, StdDev = `3134.58`|
|latency percentile (ms)|p50 = `7606.27`, p75 = `10330.11`, p95 = `13312`, p99 = `14884.86`|
|data transfer (KB)|min = `1.53`, mean = `1.53`, max = `1.53`, all = `3.7` MB|
|||
|name|`get_products`|
|request count|all = `2921`, ok = `2447`, RPS = `53.2`|
|latency (ms)|min = `3.96`, mean = `8144.08`, max = `20468.85`, StdDev = `3134.4`|
|latency percentile (ms)|p50 = `7598.08`, p75 = `10330.11`, p95 = `13303.81`, p99 = `14868.48`|
|data transfer (KB)|min = `1.388`, mean = `1.388`, max = `1.388`, all = `3.3` MB|
|||
|name|`get_customer_orders`|
|request count|all = `2447`, ok = `2447`, RPS = `53.2`|
|latency (ms)|min = `1.93`, mean = `9.89`, max = `298.3`, StdDev = `14.93`|
|latency percentile (ms)|p50 = `6.64`, p75 = `10.62`, p95 = `23.52`, p99 = `52.77`|
|data transfer (KB)|min = `0.143`, mean = `0.143`, max = `0.143`, all = `0.3` MB|


|step|failures stats|
|---|---|
|name|`global information`|
|request count|all = `2921`, fail = `474`, RPS = `10.3`|
|latency (ms)|min = `15427.42`, mean = `17782.66`, max = `22033.18`, StdDev = `1574.73`|
|latency percentile (ms)|p50 = `17432.58`, p75 = `18956.29`, p95 = `20873.22`, p99 = `21397.5`|
|||
|name|`get_products`|
|request count|all = `2921`, fail = `474`, RPS = `10.3`|
|latency (ms)|min = `15425.81`, mean = `17782.58`, max = `22033.16`, StdDev = `1574.79`|
|latency percentile (ms)|p50 = `17432.58`, p75 = `18956.29`, p95 = `20873.22`, p99 = `21397.5`|
|data transfer (KB)|min = `0.227`, mean = `0.227`, max = `0.227`, all = `0.1` MB|


> status codes for scenario: `normal_load_with_orders`



|status code|count|message|
|---|---|---|
|OK|4894||
|InternalServerError|474||


