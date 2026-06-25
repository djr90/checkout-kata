Feature: Supermarket checkout pricing
    As a checkout, I total scanned items using the current pricing rules,
    applying multi-buy offers however the items are ordered.

Background:
    Given the standard pricing rules

Scenario Outline: Totals for scanned items
    When I scan the items "<items>"
    Then the total price should be <total>

    Examples:
        | items   | total |
        | A       | 50    |
        | AAA     | 130   |
        | AAAA    | 180   |
        | AAAAAA  | 260   |
        | AAABBCD | 210   |

Scenario: Offers apply regardless of scan order
    When I scan the items "BAB"
    Then the total price should be 95

Scenario: Scanning an unknown SKU is reported to the caller
    When I scan an unknown item "Z"
    Then the scan result is a not-found failure
    And the total price should be 0
